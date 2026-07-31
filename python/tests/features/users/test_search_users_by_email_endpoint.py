from __future__ import annotations

import pytest
from httpx import ASGITransport, AsyncClient
from sqlalchemy.ext.asyncio import AsyncSession, async_sessionmaker

from app.main import app
from authentication.password_hasher import Argon2PasswordHasher
from database.session import get_db, reset_session_state
from tests.features.conftest import seed_user


@pytest.fixture()
async def search_users_by_email_client(vsa_engine) -> AsyncClient:
    reset_session_state()
    factory = async_sessionmaker(
        vsa_engine,
        class_=AsyncSession,
        autocommit=False,
        autoflush=False,
        expire_on_commit=False,
    )

    async def override_get_db():
        async with factory() as session:
            yield session

    app.dependency_overrides[get_db] = override_get_db

    transport = ASGITransport(app=app)
    client = AsyncClient(transport=transport, base_url="http://test")
    yield client
    app.dependency_overrides.clear()
    reset_session_state()


def _auth_headers(user_id: int, email: str) -> dict[str, str]:
    from authentication.config import get_jwt_settings
    from authentication.token_service import JwtTokenService
    from common.clock import SystemClock

    token_service = JwtTokenService(get_jwt_settings(), clock=SystemClock())
    token, _ = token_service.create_access_token(user_id, email)
    return {"Authorization": f"Bearer {token}"}


@pytest.mark.asyncio
async def test_search_users_by_email_without_jwt_returns_unauthorized(
    search_users_by_email_client: AsyncClient,
) -> None:
    response = await search_users_by_email_client.get(
        "/api/v1/users/search-by-email",
        params={"email": "example.com"},
    )

    assert response.status_code == 401


@pytest.mark.asyncio
async def test_search_users_by_email_with_jwt_returns_matching_users(
    search_users_by_email_client: AsyncClient,
    vsa_session: AsyncSession,
    password_hasher: Argon2PasswordHasher,
) -> None:
    admin = await seed_user(
        vsa_session,
        password_hasher,
        email="one@example.com",
        name="User One",
    )
    await seed_user(
        vsa_session,
        password_hasher,
        email="two@EXAMPLE.com",
        name="User Two",
    )
    await seed_user(
        vsa_session,
        password_hasher,
        email="other@test.com",
        name="Other",
    )
    headers = _auth_headers(admin.id, admin.email)

    response = await search_users_by_email_client.get(
        "/api/v1/users/search-by-email",
        params={"email": "example.com"},
        headers=headers,
    )

    assert response.status_code == 200
    body = response.json()
    assert isinstance(body, list)
    assert len(body) == 2


@pytest.mark.asyncio
async def test_search_users_by_email_with_jwt_returns_empty_list_when_no_match(
    search_users_by_email_client: AsyncClient,
    vsa_session: AsyncSession,
    password_hasher: Argon2PasswordHasher,
) -> None:
    admin = await seed_user(
        vsa_session,
        password_hasher,
        email="admin@example.com",
        name="Admin",
    )
    headers = _auth_headers(admin.id, admin.email)

    response = await search_users_by_email_client.get(
        "/api/v1/users/search-by-email",
        params={"email": "none@example.com"},
        headers=headers,
    )

    assert response.status_code == 200
    body = response.json()
    assert body == []
