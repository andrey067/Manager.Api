from __future__ import annotations

import pytest
from pydantic import ValidationError
from sqlalchemy import func, select
from sqlalchemy.ext.asyncio import AsyncSession

from authentication.password_hasher import Argon2PasswordHasher
from common.cache import AppCache
from database.models import UserModel
from features.users.cache_keys import UserCacheKeys
from features.users.entity import User
from features.users.events import UserCreatedDomainEvent
from features.users.register_bootstrap import BootstrapRequest, Command, Handler
from tests.features.conftest import seed_user


@pytest.mark.asyncio
async def test_handler_bootstrap_not_allowed_when_users_exist(
    vsa_session: AsyncSession,
    password_hasher: Argon2PasswordHasher,
) -> None:
    await seed_user(vsa_session, password_hasher)
    handler = Handler(vsa_session, password_hasher, AppCache())

    result = await handler.handle(
        Command(name="New User", email="new@example.com", password="Password1!")
    )

    assert result.is_failure
    assert result.error.code == "Users.BootstrapNotAllowed"
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
    await cache.get_or_set(UserCacheKeys.ALL, lambda: ["cached"], ttl_seconds=60.0)
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
            name="  Admin User  ",
            email="  admin@example.com  ",
            password=password,
        )
    )

    assert result.is_success
    assert result.value.id > 0
    assert result.value.name == "Admin User"
    assert result.value.email == "admin@example.com"

    row = (
        await vsa_session.execute(
            select(UserModel).where(UserModel.email == "admin@example.com")
        )
    ).scalar_one()
    assert row.name == "Admin User"
    assert password_hasher.verify(password, row.password)

    assert len(persisted_users) == 1
    assert len(persisted_users[0].domain_events) == 1
    event = persisted_users[0].domain_events[0]
    assert isinstance(event, UserCreatedDomainEvent)
    assert event.id == result.value.id

    refreshed = await cache.get_or_set(
        UserCacheKeys.ALL, lambda: ["refreshed"], ttl_seconds=60.0
    )
    assert refreshed == ["refreshed"]


def test_bootstrap_request_rejects_short_name() -> None:
    with pytest.raises(ValidationError):
        BootstrapRequest(name="A", email="a@b.com", password="Password1!")


def test_bootstrap_request_rejects_long_name() -> None:
    with pytest.raises(ValidationError):
        BootstrapRequest(name="a" * 81, email="user@example.com", password="Password1!")


def test_bootstrap_request_rejects_invalid_email() -> None:
    with pytest.raises(ValidationError):
        BootstrapRequest(name="Valid Name", email="not-email", password="Password1!")


def test_bootstrap_request_rejects_long_email() -> None:
    local = "a" * 170
    with pytest.raises(ValidationError):
        BootstrapRequest(
            name="Valid Name", email=f"{local}@example.com", password="Password1!"
        )


def test_bootstrap_request_rejects_short_password() -> None:
    with pytest.raises(ValidationError):
        BootstrapRequest(name="Valid Name", email="user@example.com", password="short")


def test_bootstrap_request_rejects_long_password() -> None:
    with pytest.raises(ValidationError):
        BootstrapRequest(name="Valid Name", email="user@example.com", password="a" * 31)


def test_bootstrap_request_accepts_valid_payload() -> None:
    request = BootstrapRequest(
        name="Valid Name", email="user@example.com", password="Password1!"
    )
    assert request.name == "Valid Name"
    assert request.email == "user@example.com"
    assert request.password == "Password1!"
