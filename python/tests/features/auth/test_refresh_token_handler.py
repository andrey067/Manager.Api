from __future__ import annotations

from datetime import timedelta

import pytest
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from authentication.token_service import JwtTokenService
from database.models import UserModel
from features.auth.refresh_token import Command, Handler, Validator
from tests.features.auth.conftest import FixedClock, seed_user_with_refresh_token


@pytest.mark.asyncio
async def test_handler_invalid_refresh_token_when_token_not_found(
    vsa_session: AsyncSession,
    token_service: JwtTokenService,
) -> None:
    handler = Handler(vsa_session, token_service)

    result = await handler.handle(Command(refresh_token="unknown-token"))

    assert result.is_failure
    assert result.error.code == "Auth.InvalidRefreshToken"


@pytest.mark.asyncio
async def test_handler_invalid_refresh_token_when_token_expired(
    vsa_session: AsyncSession,
    token_service: JwtTokenService,
    fixed_clock: FixedClock,
) -> None:
    refresh_token = "expired-refresh-token"
    await seed_user_with_refresh_token(
        vsa_session,
        token_service,
        refresh_token=refresh_token,
        expires_at=fixed_clock.utc_now(),
    )
    handler = Handler(vsa_session, token_service, clock=fixed_clock)

    result = await handler.handle(Command(refresh_token=refresh_token))

    assert result.is_failure
    assert result.error.code == "Auth.InvalidRefreshToken"


@pytest.mark.asyncio
async def test_handler_success_rotates_refresh_token_hash(
    vsa_session: AsyncSession,
    token_service: JwtTokenService,
    fixed_clock: FixedClock,
) -> None:
    refresh_token = "valid-refresh-token"
    user = await seed_user_with_refresh_token(
        vsa_session,
        token_service,
        refresh_token=refresh_token,
    )
    original_hash = user.refresh_token_hash
    handler = Handler(vsa_session, token_service, clock=fixed_clock)

    result = await handler.handle(Command(refresh_token=refresh_token))

    assert result.is_success
    assert result.value.access_token
    assert result.value.refresh_token
    assert result.value.refresh_token != refresh_token
    expected_refresh_expires = fixed_clock.utc_now() + timedelta(days=7)

    assert result.value.access_token_expires == fixed_clock.utc_now() + timedelta(hours=1)
    assert result.value.refresh_token_expires == expected_refresh_expires

    row = (
        await vsa_session.execute(
            select(UserModel).where(UserModel.email == "user@example.com")
        )
    ).scalar_one()
    assert row.refresh_token_hash != original_hash
    assert row.refresh_token_hash == token_service.hash_refresh_token(
        result.value.refresh_token
    )
    assert row.refresh_token_expires_at == expected_refresh_expires.replace(tzinfo=None)


def test_validator_rejects_empty_refresh_token() -> None:
    errors = Validator().validate(Command(refresh_token=""))
    assert any(e.field == "refreshToken" for e in errors)


def test_validator_accepts_valid_command() -> None:
    errors = Validator().validate(Command(refresh_token="valid-refresh-token"))
    assert errors == []
