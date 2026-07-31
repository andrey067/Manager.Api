from __future__ import annotations

from datetime import timedelta

import pytest
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from authentication.password_hasher import Argon2PasswordHasher
from authentication.token_service import JwtTokenService
from database.models import UserModel
from features.auth.login import Command, Handler, Validator
from features.users.entity import User
from features.users.events import UserLoggedInDomainEvent
from tests.features.auth.conftest import FixedClock, seed_user


@pytest.mark.asyncio
async def test_handler_invalid_credentials_when_user_not_found(
    vsa_session: AsyncSession,
    password_hasher: Argon2PasswordHasher,
    token_service: JwtTokenService,
    fixed_clock: FixedClock,
) -> None:
    handler = Handler(vsa_session, password_hasher, token_service)

    result = await handler.handle(
        Command(login="missing@example.com", password="secret")
    )

    assert result.is_failure
    assert result.error.code == "Auth.InvalidCredentials"


@pytest.mark.asyncio
async def test_handler_invalid_credentials_when_password_wrong(
    vsa_session: AsyncSession,
    password_hasher: Argon2PasswordHasher,
    token_service: JwtTokenService,
    fixed_clock: FixedClock,
) -> None:
    await seed_user(vsa_session, password_hasher, password="correct")
    handler = Handler(vsa_session, password_hasher, token_service)

    result = await handler.handle(Command(login="user@example.com", password="wrong"))

    assert result.is_failure
    assert result.error.code == "Auth.InvalidCredentials"


@pytest.mark.asyncio
async def test_handler_success_persists_refresh_token_hash(
    vsa_session: AsyncSession,
    password_hasher: Argon2PasswordHasher,
    token_service: JwtTokenService,
    fixed_clock: FixedClock,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    password = "Secret123!"
    user = await seed_user(vsa_session, password_hasher, password=password)
    handler = Handler(vsa_session, password_hasher, token_service)
    persisted_users: list[User] = []
    original_from_persistence = User.from_persistence

    def track_from_persistence(**kwargs: object) -> User:
        domain_user = original_from_persistence(**kwargs)  # type: ignore[arg-type]
        persisted_users.append(domain_user)
        return domain_user

    monkeypatch.setattr(User, "from_persistence", track_from_persistence)

    result = await handler.handle(Command(login="user@example.com", password=password))

    assert result.is_success
    assert result.value.access_token
    assert result.value.refresh_token
    expected_refresh_expires = fixed_clock.utc_now() + timedelta(days=7)

    assert result.value.access_token_expires == fixed_clock.utc_now() + timedelta(
        hours=1
    )
    assert result.value.refresh_token_expires == expected_refresh_expires

    row = (
        await vsa_session.execute(
            select(UserModel).where(UserModel.email == "user@example.com")
        )
    ).scalar_one()
    assert row.refresh_token_hash == token_service.hash_refresh_token(
        result.value.refresh_token
    )
    assert row.refresh_token_expires_at == expected_refresh_expires.replace(tzinfo=None)

    assert len(persisted_users) == 1
    assert len(persisted_users[0].domain_events) == 1
    event = persisted_users[0].domain_events[0]
    assert isinstance(event, UserLoggedInDomainEvent)
    assert event.id == user.id


def test_validator_rejects_empty_login() -> None:
    errors = Validator().validate(Command(login="", password="secret"))
    assert any(e.field == "login" for e in errors)


def test_validator_rejects_empty_password() -> None:
    errors = Validator().validate(Command(login="user@example.com", password=""))
    assert any(e.field == "password" for e in errors)


def test_validator_accepts_valid_command() -> None:
    errors = Validator().validate(Command(login="user@example.com", password="secret"))
    assert errors == []
