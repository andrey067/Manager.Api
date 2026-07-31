from __future__ import annotations

import pytest
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from authentication.password_hasher import Argon2PasswordHasher
from common.cache import AppCache
from database.models import UserModel
from features.users.cache_keys import UserCacheKeys
from features.users.entity import User
from features.users.events import UserUpdatedDomainEvent
from features.users.update_user import Command, Handler, Validator
from tests.features.conftest import seed_user


@pytest.mark.asyncio
async def test_handler_not_found_when_user_does_not_exist(
    vsa_session: AsyncSession,
    password_hasher: Argon2PasswordHasher,
) -> None:
    handler = Handler(vsa_session, password_hasher, AppCache())

    result = await handler.handle(
        Command(id=999, name="Updated", email="updated@example.com", password="Password1!")
    )

    assert result.is_failure
    assert result.error.code == "Users.NotFound"


@pytest.mark.asyncio
async def test_handler_email_conflict_when_email_belongs_to_another_user(
    vsa_session: AsyncSession,
    password_hasher: Argon2PasswordHasher,
) -> None:
    target = await seed_user(vsa_session, password_hasher, email="one@example.com", name="User One")
    await seed_user(vsa_session, password_hasher, email="two@example.com", name="User Two")
    handler = Handler(vsa_session, password_hasher, AppCache())

    result = await handler.handle(
        Command(id=target.id, name="User One", email="two@example.com", password="Password1!")
    )

    assert result.is_failure
    assert result.error.code == "Users.EmailConflict"
    row = (
        await vsa_session.execute(select(UserModel).where(UserModel.id == target.id))
    ).scalar_one()
    assert row.email == "one@example.com"


@pytest.mark.asyncio
async def test_handler_success_updates_user_raises_event_and_invalidates_cache(
    vsa_session: AsyncSession,
    password_hasher: Argon2PasswordHasher,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    cache = AppCache()
    old_email = "old@example.com"
    new_email = "new@example.com"
    password = "Password1!"
    target = await seed_user(
        vsa_session,
        password_hasher,
        email=old_email,
        name="Old Name",
        password=password,
    )

    await cache.get_or_set(UserCacheKeys.ALL, lambda: ["cached-all"], ttl_seconds=60.0)
    await cache.get_or_set(
        UserCacheKeys.by_id(target.id), lambda: ["cached-id"], ttl_seconds=60.0
    )
    await cache.get_or_set(
        UserCacheKeys.by_email(old_email), lambda: ["cached-old"], ttl_seconds=60.0
    )
    await cache.get_or_set(
        UserCacheKeys.by_email(new_email), lambda: ["cached-new"], ttl_seconds=60.0
    )

    handler = Handler(vsa_session, password_hasher, cache)
    persisted_users: list[User] = []
    original_from_persistence = User.from_persistence

    def track_from_persistence(**kwargs: object) -> User:
        user = original_from_persistence(**kwargs)  # type: ignore[arg-type]
        persisted_users.append(user)
        return user

    monkeypatch.setattr(User, "from_persistence", track_from_persistence)

    result = await handler.handle(
        Command(
            id=target.id,
            name="  New Name  ",
            email=f"  {new_email}  ",
            password=password,
        )
    )

    assert result.is_success
    assert result.value.id == target.id
    assert result.value.name == "New Name"
    assert result.value.email == new_email

    row = (
        await vsa_session.execute(select(UserModel).where(UserModel.id == target.id))
    ).scalar_one()
    assert row.name == "New Name"
    assert row.email == new_email
    assert password_hasher.verify(password, row.password)

    assert len(persisted_users) == 1
    assert len(persisted_users[0].domain_events) == 1
    event = persisted_users[0].domain_events[0]
    assert isinstance(event, UserUpdatedDomainEvent)
    assert event.id == target.id

    assert await cache.get_or_set(
        UserCacheKeys.ALL, lambda: ["refreshed-all"], ttl_seconds=60.0
    ) == ["refreshed-all"]
    assert await cache.get_or_set(
        UserCacheKeys.by_id(target.id), lambda: ["refreshed-id"], ttl_seconds=60.0
    ) == ["refreshed-id"]
    assert await cache.get_or_set(
        UserCacheKeys.by_email(old_email), lambda: ["refreshed-old"], ttl_seconds=60.0
    ) == ["refreshed-old"]
    assert await cache.get_or_set(
        UserCacheKeys.by_email(new_email), lambda: ["refreshed-new"], ttl_seconds=60.0
    ) == ["refreshed-new"]


def test_validator_rejects_short_name() -> None:
    errors = Validator().validate(
        Command(id=1, name="A", email="user@example.com", password="Password1!")
    )
    assert any(e.field == "name" for e in errors)


def test_validator_rejects_long_name() -> None:
    errors = Validator().validate(
        Command(id=1, name="a" * 81, email="user@example.com", password="Password1!")
    )
    assert any(e.field == "name" for e in errors)


def test_validator_rejects_invalid_email() -> None:
    errors = Validator().validate(
        Command(id=1, name="Valid Name", email="not-email", password="Password1!")
    )
    assert any(e.field == "email" for e in errors)


def test_validator_rejects_long_email() -> None:
    local = "a" * 170
    errors = Validator().validate(
        Command(id=1, name="Valid Name", email=f"{local}@example.com", password="Password1!")
    )
    assert any(e.field == "email" for e in errors)


def test_validator_rejects_short_password() -> None:
    errors = Validator().validate(
        Command(id=1, name="Valid Name", email="user@example.com", password="short")
    )
    assert any(e.field == "password" for e in errors)


def test_validator_rejects_long_password() -> None:
    errors = Validator().validate(
        Command(id=1, name="Valid Name", email="user@example.com", password="a" * 31)
    )
    assert any(e.field == "password" for e in errors)


def test_validator_accepts_valid_command() -> None:
    errors = Validator().validate(
        Command(id=1, name="Valid Name", email="user@example.com", password="Password1!")
    )
    assert errors == []
