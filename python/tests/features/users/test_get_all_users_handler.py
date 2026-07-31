from __future__ import annotations

import pytest
from sqlalchemy import func, select
from sqlalchemy.ext.asyncio import AsyncSession

from authentication.password_hasher import Argon2PasswordHasher
from common.cache import AppCache
from database.models import UserModel
from features.users.cache_keys import UserCacheKeys
from features.users.create_user import Command as CreateUserCommand, Handler as CreateUserHandler
from features.users.get_all_users import Handler, Query, Response
from tests.features.conftest import seed_user


@pytest.mark.asyncio
async def test_handler_success_loads_from_database_and_caches(
    vsa_session: AsyncSession,
    password_hasher: Argon2PasswordHasher,
) -> None:
    cache = AppCache()
    await seed_user(vsa_session, password_hasher, email="alice@example.com", name="Alice")
    await seed_user(vsa_session, password_hasher, email="bob@example.com", name="Bob")
    handler = Handler(vsa_session, cache)

    result = await handler.handle(Query())

    assert result.is_success
    assert len(result.value) == 2
    emails = {user.email for user in result.value}
    assert emails == {"alice@example.com", "bob@example.com"}

    cached = await cache.get_or_set(
        UserCacheKeys.ALL,
        lambda: [],
        ttl_seconds=60.0,
    )
    assert len(cached) == 2


@pytest.mark.asyncio
async def test_handler_cache_hit_returns_cached_without_database(
    vsa_session: AsyncSession,
) -> None:
    cache = AppCache()
    cached_list = [Response(id=1, name="From Cache", email="cache@example.com")]
    await cache.get_or_set(UserCacheKeys.ALL, lambda: cached_list, ttl_seconds=60.0)
    handler = Handler(vsa_session, cache)

    result = await handler.handle(Query())

    assert result.is_success
    assert result.value == cached_list
    count = (
        await vsa_session.execute(select(func.count()).select_from(UserModel))
    ).scalar_one()
    assert count == 0


@pytest.mark.asyncio
async def test_handler_after_create_user_invalidation_refreshes_list(
    vsa_session: AsyncSession,
    password_hasher: Argon2PasswordHasher,
) -> None:
    cache = AppCache()
    await seed_user(vsa_session, password_hasher, email="existing@example.com", name="Existing")
    get_all_handler = Handler(vsa_session, cache)
    create_handler = CreateUserHandler(vsa_session, password_hasher, cache)

    first = await get_all_handler.handle(Query())
    assert first.is_success
    assert len(first.value) == 1

    created = await create_handler.handle(
        CreateUserCommand(
            name="New User",
            email="new@example.com",
            password="Password1!",
        )
    )
    assert created.is_success

    second = await get_all_handler.handle(Query())
    assert second.is_success
    assert len(second.value) == 2
    assert any(user.email == "new@example.com" for user in second.value)
