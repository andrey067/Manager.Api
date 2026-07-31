from __future__ import annotations

import pytest
from httpx import ASGITransport, AsyncClient
from sqlalchemy.ext.asyncio import AsyncSession, async_sessionmaker

from app.main import app
from authentication.token_service import JwtTokenService
from database.session import get_db, reset_session_state
from tests.features.auth.conftest import seed_user_with_refresh_token


@pytest.fixture()
async def refresh_client(
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
async def test_post_refresh_empty_refresh_token_returns_validation_error(
    refresh_client: AsyncClient,
) -> None:
    response = await refresh_client.post(
        "/api/v1/auth/refresh",
        json={"refreshToken": ""},
    )

    assert response.status_code == 400
    body = response.json()
    assert body["errorCode"] == "Validation.Error"


@pytest.mark.asyncio
async def test_post_refresh_invalid_token_returns_problem(
    refresh_client: AsyncClient,
) -> None:
    response = await refresh_client.post(
        "/api/v1/auth/refresh",
        json={"refreshToken": "invalid-token"},
    )

    assert response.status_code == 400
    body = response.json()
    assert body["errorCode"] == "Auth.InvalidRefreshToken"


@pytest.mark.asyncio
async def test_post_refresh_valid_token_returns_rotated_tokens(
    refresh_client: AsyncClient,
    vsa_session: AsyncSession,
    token_service: JwtTokenService,
) -> None:
    refresh_token = "stored-refresh-token"
    await seed_user_with_refresh_token(
        vsa_session,
        token_service,
        refresh_token=refresh_token,
    )

    response = await refresh_client.post(
        "/api/v1/auth/refresh",
        json={"refreshToken": refresh_token},
    )

    assert response.status_code == 200
    body = response.json()
    assert body["accessToken"]
    assert body["refreshToken"]
    assert body["refreshToken"] != refresh_token
    assert body["accessTokenExpires"]
    assert body["refreshTokenExpires"]
