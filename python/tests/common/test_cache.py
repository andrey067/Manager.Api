import pytest

from common.cache import AppCache


@pytest.mark.asyncio
async def test_get_or_set_and_remove():
    cache = AppCache()
    v1 = await cache.get_or_set("users:1", lambda: {"id": 1}, ttl_seconds=60)
    v2 = await cache.get_or_set("users:1", lambda: {"id": 99}, ttl_seconds=60)
    assert v1 == v2 == {"id": 1}
    await cache.remove("users:1")
    v3 = await cache.get_or_set("users:1", lambda: {"id": 2}, ttl_seconds=60)
    assert v3 == {"id": 2}
