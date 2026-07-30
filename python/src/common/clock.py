from __future__ import annotations

from datetime import UTC, datetime
from typing import Protocol


class Clock(Protocol):
    def utc_now(self) -> datetime:
        """Return the current UTC time as a timezone-aware datetime."""
        ...


class SystemClock:
    def utc_now(self) -> datetime:
        return datetime.now(UTC)
