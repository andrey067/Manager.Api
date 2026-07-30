from __future__ import annotations

import asyncio
from collections.abc import Awaitable, Callable
from dataclasses import dataclass
from datetime import datetime, timedelta
from typing import Any

from common.clock import Clock, SystemClock

Factory = Callable[[], Any] | Callable[[], Awaitable[Any]]


@dataclass(slots=True)
class _CacheEntry:
    value: Any
    expires_at: datetime


class AppCache:
    def __init__(self, clock: Clock | None = None) -> None:
        self._clock = clock or SystemClock()
        self._entries: dict[str, _CacheEntry] = {}

    async def get_or_set(
        self,
        key: str,
        factory: Factory,
        ttl_seconds: float,
    ) -> Any:
        now = self._clock.utc_now()
        entry = self._entries.get(key)
        if entry is not None and entry.expires_at > now:
            return entry.value

        value = await self._invoke_factory(factory)
        expires_at = now + timedelta(seconds=ttl_seconds)
        self._entries[key] = _CacheEntry(value=value, expires_at=expires_at)
        return value

    async def remove(self, key: str) -> None:
        self._entries.pop(key, None)

    async def remove_by_prefix(self, prefix: str) -> None:
        for key in list(self._entries):
            if key.startswith(prefix):
                del self._entries[key]

    async def _invoke_factory(self, factory: Factory) -> Any:
        result = factory()
        if asyncio.iscoroutine(result):
            return await result
        return result
