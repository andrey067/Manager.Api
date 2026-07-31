from __future__ import annotations

import pytest
from httpx import ASGITransport, AsyncClient
from sqlalchemy import delete
from sqlalchemy.ext.asyncio import AsyncSession, async_sessionmaker

from app.main import app
from authentication.password_hasher import Argon2PasswordHasher
from database.models import UserModel
from database.session import get_db, reset_session_state
from tests.features.conftest import seed_user


@pytest.fixture()
async def bootstrap_client(vsa_engine) -> AsyncClient:
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


@pytest.mark.asyncio
async def test_post_bootstrap_invalid_name_returns_validation_error(
    bootstrap_client: AsyncClient,
) -> None:
    response = await bootstrap_client.post(
        "/api/v1/users/bootstrap",
        json={"name": "A", "email": "user@example.com", "password": "Password1!"},
    )

    assert response.status_code == 422


@pytest.mark.asyncio
async def test_post_bootstrap_when_users_exist_returns_bootstrap_not_allowed(
    bootstrap_client: AsyncClient,
    vsa_session: AsyncSession,
    password_hasher: Argon2PasswordHasher,
) -> None:
    await seed_user(vsa_session, password_hasher, email="existing@example.com")

    response = await bootstrap_client.post(
        "/api/v1/users/bootstrap",
        json={"name": "New User", "email": "new@example.com", "password": "Password1!"},
    )

    assert response.status_code == 400
    body = response.json()
    assert body["errorCode"] == "Users.BootstrapNotAllowed"


@pytest.mark.asyncio
async def test_post_bootstrap_when_empty_creates_user(
    bootstrap_client: AsyncClient,
    vsa_session: AsyncSession,
) -> None:
    await vsa_session.execute(delete(UserModel))
    await vsa_session.commit()

    response = await bootstrap_client.post(
        "/api/v1/users/bootstrap",
        json={
            "name": "Bootstrap Admin",
            "email": "bootstrap@example.com",
            "password": "Password1!",
        },
    )

    assert response.status_code == 200
    body = response.json()
    assert body["id"] > 0
    assert body["name"] == "Bootstrap Admin"
    assert body["email"] == "bootstrap@example.com"
