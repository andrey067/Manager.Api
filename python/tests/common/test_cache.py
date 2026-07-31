from datetime import UTC, datetime

import pytest

from common.cache import AppCache


class FixedClock:
    def __init__(self, instant: datetime) -> None:
        self._instant = instant

    def utc_now(self) -> datetime:
        return self._instant


@pytest.mark.asyncio
async def test_get_or_set_does_not_cache_none() -> None:
    clock = FixedClock(datetime(2026, 7, 31, tzinfo=UTC))
    cache = AppCache(clock=clock)
    calls = {"n": 0}

    async def factory() -> None:
        calls["n"] += 1
        return None

    assert await cache.get_or_set("k", factory, ttl_seconds=60) is None
    assert await cache.get_or_set("k", factory, ttl_seconds=60) is None
    assert calls["n"] == 2


@pytest.mark.asyncio
async def test_get_or_set_and_remove():
    cache = AppCache()
    v1 = await cache.get_or_set("users:1", lambda: {"id": 1}, ttl_seconds=60)
    v2 = await cache.get_or_set("users:1", lambda: {"id": 99}, ttl_seconds=60)
    assert v1 == v2 == {"id": 1}
    await cache.remove("users:1")
    v3 = await cache.get_or_set("users:1", lambda: {"id": 2}, ttl_seconds=60)
    assert v3 == {"id": 2}
