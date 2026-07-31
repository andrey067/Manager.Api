from __future__ import annotations

import pytest
from httpx import ASGITransport, AsyncClient
from sqlalchemy.ext.asyncio import AsyncSession, async_sessionmaker

from app.main import app
from authentication.password_hasher import Argon2PasswordHasher
from database.session import get_db, reset_session_state
from tests.features.conftest import seed_user


@pytest.fixture()
async def update_user_client(vsa_engine) -> AsyncClient:
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
async def test_put_users_without_jwt_returns_unauthorized(
    update_user_client: AsyncClient,
) -> None:
    response = await update_user_client.put(
        "/api/v1/users/1",
        json={"name": "Updated", "email": "updated@example.com", "password": "Password1!"},
    )

    assert response.status_code == 401


@pytest.mark.asyncio
async def test_put_users_with_jwt_updates_user(
    update_user_client: AsyncClient,
    vsa_session: AsyncSession,
    password_hasher: Argon2PasswordHasher,
) -> None:
    target = await seed_user(vsa_session, password_hasher, email="target@example.com", name="Target")
    admin = await seed_user(vsa_session, password_hasher, email="admin@example.com")
    headers = _auth_headers(admin.id, admin.email)

    response = await update_user_client.put(
        f"/api/v1/users/{target.id}",
        json={
            "name": "Updated User",
            "email": "updated@example.com",
            "password": "Password1!",
        },
        headers=headers,
    )

    assert response.status_code == 200
    body = response.json()
    assert body["id"] == target.id
    assert body["name"] == "Updated User"
    assert body["email"] == "updated@example.com"


@pytest.mark.asyncio
async def test_put_users_not_found_returns_not_found(
    update_user_client: AsyncClient,
    vsa_session: AsyncSession,
    password_hasher: Argon2PasswordHasher,
) -> None:
    admin = await seed_user(vsa_session, password_hasher, email="admin@example.com")
    headers = _auth_headers(admin.id, admin.email)

    response = await update_user_client.put(
        "/api/v1/users/999",
        json={"name": "Missing", "email": "missing@example.com", "password": "Password1!"},
        headers=headers,
    )

    assert response.status_code == 404
    body = response.json()
    assert body["errorCode"] == "Users.NotFound"


@pytest.mark.asyncio
async def test_put_users_duplicate_email_returns_email_conflict(
    update_user_client: AsyncClient,
    vsa_session: AsyncSession,
    password_hasher: Argon2PasswordHasher,
) -> None:
    target = await seed_user(vsa_session, password_hasher, email="target@example.com", name="Target")
    await seed_user(vsa_session, password_hasher, email="existing@example.com", name="Existing")
    admin = await seed_user(vsa_session, password_hasher, email="admin@example.com")
    headers = _auth_headers(admin.id, admin.email)

    response = await update_user_client.put(
        f"/api/v1/users/{target.id}",
        json={
            "name": "Duplicate",
            "email": "existing@example.com",
            "password": "Password1!",
        },
        headers=headers,
    )

    assert response.status_code == 409
    body = response.json()
    assert body["errorCode"] == "Users.EmailConflict"
