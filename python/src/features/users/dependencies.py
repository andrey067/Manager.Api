"""Shared DI helpers for user feature slices."""

from __future__ import annotations

from common.cache import AppCache

_app_cache: AppCache | None = None


def get_app_cache() -> AppCache:
    global _app_cache
    if _app_cache is None:
        _app_cache = AppCache()
    return _app_cache
