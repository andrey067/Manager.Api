from __future__ import annotations

import pytest
from sqlalchemy import func, select
from sqlalchemy.ext.asyncio import AsyncSession

from authentication.password_hasher import Argon2PasswordHasher
from common.cache import AppCache
from database.models import UserModel
from features.users.cache_keys import UserCacheKeys
from features.users.entity import User
from features.users.events import UserCreatedDomainEvent
from features.users.create_user import Command, Handler, Validator
from tests.features.conftest import seed_user


@pytest.mark.asyncio
async def test_handler_email_conflict_when_email_exists(
    vsa_session: AsyncSession,
    password_hasher: Argon2PasswordHasher,
) -> None:
    await seed_user(vsa_session, password_hasher, email="existing@example.com")
    handler = Handler(vsa_session, password_hasher, AppCache())

    result = await handler.handle(
        Command(name="New User", email="existing@example.com", password="Password1!")
    )

    assert result.is_failure
    assert result.error.code == "Users.EmailConflict"
    count = (
        await vsa_session.execute(select(func.count()).select_from(UserModel))
    ).scalar_one()
    assert count == 1


@pytest.mark.asyncio
async def test_handler_success_creates_user_raises_event_and_invalidates_cache(
    vsa_session: AsyncSession,
    password_hasher: Argon2PasswordHasher,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    cache = AppCache()
    email = "new@example.com"
    await cache.get_or_set(UserCacheKeys.ALL, lambda: ["cached"], ttl_seconds=60.0)
    await cache.get_or_set(
        UserCacheKeys.by_email(email), lambda: ["cached-email"], ttl_seconds=60.0
    )
    handler = Handler(vsa_session, password_hasher, cache)
    password = "Password1!"
    persisted_users: list[User] = []
    original_from_persistence = User.from_persistence

    def track_from_persistence(**kwargs: object) -> User:
        user = original_from_persistence(**kwargs)  # type: ignore[arg-type]
        persisted_users.append(user)
        return user

    monkeypatch.setattr(User, "from_persistence", track_from_persistence)

    result = await handler.handle(
        Command(
            name="  New User  ",
            email=f"  {email}  ",
            password=password,
        )
    )

    assert result.is_success
    assert result.value.id > 0
    assert result.value.name == "New User"
    assert result.value.email == email

    row = (
        await vsa_session.execute(select(UserModel).where(UserModel.email == email))
    ).scalar_one()
    assert row.name == "New User"
    assert password_hasher.verify(password, row.password)

    assert len(persisted_users) == 1
    assert len(persisted_users[0].domain_events) == 1
    event = persisted_users[0].domain_events[0]
    assert isinstance(event, UserCreatedDomainEvent)
    assert event.id == result.value.id

    refreshed_all = await cache.get_or_set(
        UserCacheKeys.ALL, lambda: ["refreshed-all"], ttl_seconds=60.0
    )
    assert refreshed_all == ["refreshed-all"]

    refreshed_email = await cache.get_or_set(
        UserCacheKeys.by_email(email), lambda: ["refreshed-email"], ttl_seconds=60.0
    )
    assert refreshed_email == ["refreshed-email"]


def test_validator_rejects_short_name() -> None:
    errors = Validator().validate(
        Command(name="A", email="user@example.com", password="Password1!")
    )
    assert any(e.field == "name" for e in errors)


def test_validator_rejects_long_name() -> None:
    errors = Validator().validate(
        Command(name="a" * 81, email="user@example.com", password="Password1!")
    )
    assert any(e.field == "name" for e in errors)


def test_validator_rejects_invalid_email() -> None:
    errors = Validator().validate(
        Command(name="Valid Name", email="not-email", password="Password1!")
    )
    assert any(e.field == "email" for e in errors)


def test_validator_rejects_long_email() -> None:
    local = "a" * 170
    errors = Validator().validate(
        Command(name="Valid Name", email=f"{local}@example.com", password="Password1!")
    )
    assert any(e.field == "email" for e in errors)


def test_validator_rejects_short_password() -> None:
    errors = Validator().validate(
        Command(name="Valid Name", email="user@example.com", password="short")
    )
    assert any(e.field == "password" for e in errors)


def test_validator_rejects_long_password() -> None:
    errors = Validator().validate(
        Command(name="Valid Name", email="user@example.com", password="a" * 31)
    )
    assert any(e.field == "password" for e in errors)


def test_validator_accepts_valid_command() -> None:
    errors = Validator().validate(
        Command(name="Valid Name", email="user@example.com", password="Password1!")
    )
    assert errors == []
