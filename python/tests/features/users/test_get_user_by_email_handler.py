from __future__ import annotations

import pytest
from sqlalchemy import func, select
from sqlalchemy.ext.asyncio import AsyncSession

from authentication.password_hasher import Argon2PasswordHasher
from common.cache import AppCache
from database.models import UserModel
from features.users.cache_keys import UserCacheKeys
from features.users.get_user_by_email import Handler, Query, Response
from tests.features.conftest import seed_user


@pytest.mark.asyncio
async def test_handler_not_found_when_user_does_not_exist(
    vsa_session: AsyncSession,
) -> None:
    handler = Handler(vsa_session, AppCache())

    result = await handler.handle(Query(email="missing@example.com"))

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

    result = await handler.handle(Query(email="cached@example.com"))

    assert result.is_success
    assert result.value.id == user.id
    assert result.value.name == "Cached User"
    assert result.value.email == "cached@example.com"

    cached = await cache.get_or_set(
        UserCacheKeys.by_email("cached@example.com"),
        lambda: Response(id=-1, name="stale", email="stale@example.com"),
        ttl_seconds=60.0,
    )
    assert cached.id == user.id
    assert cached.name == "Cached User"


@pytest.mark.asyncio
async def test_handler_not_found_does_not_poison_cache_allows_later_success(
    vsa_session: AsyncSession,
    password_hasher: Argon2PasswordHasher,
) -> None:
    cache = AppCache()
    handler = Handler(vsa_session, cache)

    miss = await handler.handle(Query(email="later@example.com"))
    assert miss.is_failure

    await seed_user(
        vsa_session,
        password_hasher,
        email="later@example.com",
        name="Later",
    )

    hit = await handler.handle(Query(email="later@example.com"))
    assert hit.is_success
    assert hit.value.email == "later@example.com"


@pytest.mark.asyncio
async def test_handler_cache_hit_returns_cached_without_database(
    vsa_session: AsyncSession,
) -> None:
    cache = AppCache()
    cached_response = Response(id=42, name="From Cache", email="cache@example.com")
    await cache.get_or_set(
        UserCacheKeys.by_email("cache@example.com"),
        lambda: cached_response,
        ttl_seconds=60.0,
    )
    handler = Handler(vsa_session, cache)

    result = await handler.handle(Query(email="cache@example.com"))

    assert result.is_success
    assert result.value == cached_response
    count = (
        await vsa_session.execute(select(func.count()).select_from(UserModel))
    ).scalar_one()
    assert count == 0


@pytest.mark.asyncio
async def test_handler_email_match_is_case_insensitive(
    vsa_session: AsyncSession,
    password_hasher: Argon2PasswordHasher,
) -> None:
    user = await seed_user(
        vsa_session,
        password_hasher,
        email="User@Example.COM",
        name="Case User",
    )
    handler = Handler(vsa_session, AppCache())

    result = await handler.handle(Query(email="user@example.com"))

    assert result.is_success
    assert result.value.id == user.id
