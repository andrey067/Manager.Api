from __future__ import annotations

import pytest
from httpx import ASGITransport, AsyncClient
from sqlalchemy.ext.asyncio import AsyncSession, async_sessionmaker

from app.main import app
from authentication.password_hasher import Argon2PasswordHasher
from database.session import get_db, reset_session_state
from tests.features.auth.conftest import seed_user


@pytest.fixture()
async def login_client(
    vsa_engine,
) -> AsyncClient:
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
@pytest.mark.parametrize(
    ("login", "password"),
    [
        ("", "password"),
        ("user@example.com", ""),
    ],
)
async def test_post_login_empty_login_or_password_returns_validation_error(
    login_client: AsyncClient,
    login: str,
    password: str,
) -> None:
    response = await login_client.post(
        "/api/v1/auth/login",
        json={"login": login, "password": password},
    )

    assert response.status_code == 400
    body = response.json()
    assert body["errorCode"] == "Validation.Error"


@pytest.mark.asyncio
async def test_post_login_invalid_credentials_returns_problem(
    login_client: AsyncClient,
) -> None:
    response = await login_client.post(
        "/api/v1/auth/login",
        json={"login": "nobody@example.com", "password": "wrong"},
    )

    assert response.status_code == 400
    body = response.json()
    assert body["errorCode"] == "Auth.InvalidCredentials"


@pytest.mark.asyncio
async def test_post_login_success_returns_tokens(
    login_client: AsyncClient,
    vsa_session: AsyncSession,
    password_hasher: Argon2PasswordHasher,
) -> None:
    await seed_user(vsa_session, password_hasher)

    response = await login_client.post(
        "/api/v1/auth/login",
        json={"login": "user@example.com", "password": "Secret123!"},
    )

    assert response.status_code == 200
    body = response.json()
    assert body["accessToken"]
    assert body["refreshToken"]
    assert body["accessTokenExpires"]
    assert body["refreshTokenExpires"]
