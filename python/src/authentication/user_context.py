from __future__ import annotations

from typing import Protocol

from starlette.requests import Request


class UserContext(Protocol):
    @property
    def user_id(self) -> int | None:
        """Authenticated user id from JWT claims, or None when anonymous."""
        ...


class HttpUserContext:
    def __init__(self, request: Request) -> None:
        self._request = request

    @property
    def user_id(self) -> int | None:
        subject = self._request.state.user_id
        if subject is None:
            return None
        try:
            return int(subject)
        except (TypeError, ValueError):
            return None
