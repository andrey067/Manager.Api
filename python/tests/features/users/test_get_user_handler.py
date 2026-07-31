from __future__ import annotations

import pytest
from sqlalchemy import func, select
from sqlalchemy.ext.asyncio import AsyncSession

from authentication.password_hasher import Argon2PasswordHasher
from common.cache import AppCache
from database.models import UserModel
from features.users.cache_keys import UserCacheKeys
from features.users.get_user import Handler, Query, Response
from tests.features.conftest import seed_user


@pytest.mark.asyncio
async def test_handler_not_found_when_user_does_not_exist(
    vsa_session: AsyncSession,
) -> None:
    handler = Handler(vsa_session, AppCache())

    result = await handler.handle(Query(id=99))

    assert result.is_failure
    assert result.error.code == "Users.NotFound"


@pytest.mark.asyncio
async def test_handler_success_loads_from_database_and_caches(
    vsa_session: AsyncSession,
    password_hasher: Argon2PasswordHasher,
) -> None:
    cache = AppCache()
    user = await seed_user(
        vsa_session,
        password_hasher,
        email="cached@example.com",
        name="Cached User",
    )
    handler = Handler(vsa_session, cache)

    result = await handler.handle(Query(id=user.id))

    assert result.is_success
    assert result.value.id == user.id
    assert result.value.name == "Cached User"
    assert result.value.email == "cached@example.com"

    cached = await cache.get_or_set(
        UserCacheKeys.by_id(user.id),
        lambda: Response(id=-1, name="stale", email="stale@example.com"),
        ttl_seconds=60.0,
    )
    assert cached.id == user.id
    assert cached.name == "Cached User"


@pytest.mark.asyncio
async def test_handler_cache_hit_returns_cached_without_database(
    vsa_session: AsyncSession,
) -> None:
    cache = AppCache()
    cached_response = Response(id=42, name="From Cache", email="cache@example.com")
    await cache.get_or_set(
        UserCacheKeys.by_id(42),
        lambda: cached_response,
        ttl_seconds=60.0,
    )
    handler = Handler(vsa_session, cache)

    result = await handler.handle(Query(id=42))

    assert result.is_success
    assert result.value == cached_response
    count = (
        await vsa_session.execute(select(func.count()).select_from(UserModel))
    ).scalar_one()
    assert count == 0
