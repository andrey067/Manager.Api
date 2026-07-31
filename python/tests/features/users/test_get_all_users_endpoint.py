from __future__ import annotations

import pytest
from httpx import ASGITransport, AsyncClient
from sqlalchemy.ext.asyncio import AsyncSession, async_sessionmaker

from app.main import app
from authentication.password_hasher import Argon2PasswordHasher
from database.session import get_db, reset_session_state
from tests.features.conftest import seed_user


@pytest.fixture()
async def get_all_users_client(vsa_engine) -> AsyncClient:
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
async def test_get_users_without_jwt_returns_unauthorized(
    get_all_users_client: AsyncClient,
) -> None:
    response = await get_all_users_client.get("/api/v1/users")

    assert response.status_code == 401


@pytest.mark.asyncio
async def test_get_users_with_jwt_returns_all_users(
    get_all_users_client: AsyncClient,
    vsa_session: AsyncSession,
    password_hasher: Argon2PasswordHasher,
) -> None:
    admin = await seed_user(vsa_session, password_hasher, email="admin@example.com", name="Admin User")
    await seed_user(vsa_session, password_hasher, email="other@example.com", name="Other User")
    headers = _auth_headers(admin.id, admin.email)

    response = await get_all_users_client.get("/api/v1/users", headers=headers)

    assert response.status_code == 200
    body = response.json()
    assert isinstance(body, list)
    assert len(body) == 2


@pytest.mark.asyncio
async def test_get_users_after_create_user_includes_new_user(
    get_all_users_client: AsyncClient,
    vsa_session: AsyncSession,
    password_hasher: Argon2PasswordHasher,
) -> None:
    import uuid

    admin_email = f"getall-cache-admin-{uuid.uuid4().hex}@example.com"
    admin = await seed_user(
        vsa_session,
        password_hasher,
        email=admin_email,
        name="Admin User",
    )
    headers = _auth_headers(admin.id, admin.email)
    new_email = f"created-{uuid.uuid4().hex}@example.com"

    before = await get_all_users_client.get("/api/v1/users", headers=headers)
    assert before.status_code == 200
    before_emails = {user["email"] for user in before.json()}
    assert new_email not in before_emails

    create_response = await get_all_users_client.post(
        "/api/v1/users",
        json={
            "name": "Created User",
            "email": new_email,
            "password": "Password1!",
        },
        headers=headers,
    )
    assert create_response.status_code == 200

    after = await get_all_users_client.get("/api/v1/users", headers=headers)
    assert after.status_code == 200
    after_emails = {user["email"] for user in after.json()}
    assert new_email in after_emails
