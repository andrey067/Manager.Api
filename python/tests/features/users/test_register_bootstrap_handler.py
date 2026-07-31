from __future__ import annotations

import pytest
from sqlalchemy import func, select
from sqlalchemy.ext.asyncio import AsyncSession

from authentication.password_hasher import Argon2PasswordHasher
from common.cache import AppCache
from database.models import UserModel
from features.users.cache_keys import UserCacheKeys
from features.users.register_bootstrap import Command, Handler, Validator
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
async def test_handler_success_creates_user_and_invalidates_cache(
    vsa_session: AsyncSession,
    password_hasher: Argon2PasswordHasher,
) -> None:
    cache = AppCache()
    await cache.get_or_set(UserCacheKeys.ALL, lambda: ["cached"], ttl_seconds=60.0)
    handler = Handler(vsa_session, password_hasher, cache)
    password = "Password1!"

    result = await handler.handle(
        Command(name="Admin User", email="admin@example.com", password=password)
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

    refreshed = await cache.get_or_set(
        UserCacheKeys.ALL, lambda: ["refreshed"], ttl_seconds=60.0
    )
    assert refreshed == ["refreshed"]


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
