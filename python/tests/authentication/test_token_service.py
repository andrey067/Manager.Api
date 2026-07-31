from __future__ import annotations

import hashlib
from datetime import UTC, datetime, timedelta

from authentication.config import JwtSettings
from authentication.token_service import JwtTokenService


class FixedClock:
    def __init__(self, now: datetime | None = None) -> None:
        self._now = now or datetime(2026, 7, 30, 12, 0, 0, tzinfo=UTC)

    def utc_now(self) -> datetime:
        return self._now


def _service(clock: FixedClock | None = None) -> JwtTokenService:
    return JwtTokenService(
        JwtSettings(
            secret="test-signing-key-32-chars-minimum!!",
            issuer="Manager.Api",
            audience="Manager.Api",
            hours_to_expire=2,
            refresh_days_to_expire=7,
        ),
        clock=clock or FixedClock(),
    )


def test_hash_refresh_token_produces_stable_sha256_hex() -> None:
    svc = _service()
    raw = "refresh-token-value"
    expected = hashlib.sha256(raw.encode("utf-8")).hexdigest().upper()
    assert svc.hash_refresh_token(raw) == expected
    assert svc.hash_refresh_token(raw) == expected


def test_create_access_token_uses_clock_for_expiry() -> None:
    clock = FixedClock()
    svc = _service(clock)
    token, expires = svc.create_access_token(42, "user@example.com")
    assert token
    assert expires == clock.utc_now() + timedelta(hours=2)
    user_id = svc.validate_access_token(token)
    assert user_id == 42


def test_get_access_expiry_uses_clock() -> None:
    clock = FixedClock()
    svc = _service(clock)
    assert svc.get_access_expiry() == clock.utc_now() + timedelta(hours=2)


def test_get_refresh_expiry_uses_clock() -> None:
    clock = FixedClock()
    svc = _service(clock)
    assert svc.get_refresh_expiry() == clock.utc_now() + timedelta(days=7)


def test_create_refresh_token_returns_base64() -> None:
    import base64

    svc = _service()
    token = svc.create_refresh_token()
    assert token
    assert len(base64.b64decode(token)) == 64
