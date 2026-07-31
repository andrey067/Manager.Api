from __future__ import annotations

from features.users.cache_keys import UserCacheKeys


def test_all_key() -> None:
    assert UserCacheKeys.ALL == "users:all"


def test_by_id_key() -> None:
    assert UserCacheKeys.by_id(42) == "users:42"


def test_by_email_key_lower_invariant() -> None:
    assert UserCacheKeys.by_email("User@Example.COM") == "users:email:user@example.com"
