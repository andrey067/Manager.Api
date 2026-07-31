from __future__ import annotations

import pytest
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from authentication.password_hasher import Argon2PasswordHasher
from common.cache import AppCache
from database.models import UserModel
from features.users.cache_keys import UserCacheKeys
from features.users.entity import User
from features.users.events import UserRemovedDomainEvent
from features.users.remove_user import Command, Handler
from tests.features.conftest import seed_user


@pytest.mark.asyncio
async def test_handler_not_found_when_user_does_not_exist(
    vsa_session: AsyncSession,
    password_hasher: Argon2PasswordHasher,
) -> None:
    handler = Handler(vsa_session, AppCache())

    result = await handler.handle(Command(id=999))

    assert result.is_failure
    assert result.error.code == "Users.NotFound"


@pytest.mark.asyncio
async def test_handler_success_removes_user_raises_event_and_invalidates_cache(
    vsa_session: AsyncSession,
    password_hasher: Argon2PasswordHasher,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    cache = AppCache()
    email = "remove@example.com"
    target = await seed_user(
        vsa_session,
        password_hasher,
        email=email,
        name="Remove Me",
        password="Password1!",
    )

    await cache.get_or_set(UserCacheKeys.ALL, lambda: ["cached-all"], ttl_seconds=60.0)
    await cache.get_or_set(
        UserCacheKeys.by_id(target.id), lambda: ["cached-id"], ttl_seconds=60.0
    )
    await cache.get_or_set(
        UserCacheKeys.by_email(email), lambda: ["cached-email"], ttl_seconds=60.0
    )

    handler = Handler(vsa_session, cache)
    persisted_users: list[User] = []
    original_from_persistence = User.from_persistence

    def track_from_persistence(**kwargs: object) -> User:
        user = original_from_persistence(**kwargs)  # type: ignore[arg-type]
        persisted_users.append(user)
        return user

    monkeypatch.setattr(User, "from_persistence", track_from_persistence)

    result = await handler.handle(Command(id=target.id))

    assert result.is_success
    row = (
        await vsa_session.execute(select(UserModel).where(UserModel.id == target.id))
    ).scalar_one_or_none()
    assert row is None

    assert len(persisted_users) == 1
    assert len(persisted_users[0].domain_events) == 1
    event = persisted_users[0].domain_events[0]
    assert isinstance(event, UserRemovedDomainEvent)
    assert event.id == target.id

    assert await cache.get_or_set(
        UserCacheKeys.ALL, lambda: ["refreshed-all"], ttl_seconds=60.0
    ) == ["refreshed-all"]
    assert await cache.get_or_set(
        UserCacheKeys.by_id(target.id), lambda: ["refreshed-id"], ttl_seconds=60.0
    ) == ["refreshed-id"]
    assert await cache.get_or_set(
        UserCacheKeys.by_email(email), lambda: ["refreshed-email"], ttl_seconds=60.0
    ) == ["refreshed-email"]
