"""VSA user domain entity."""

from __future__ import annotations

from datetime import datetime

from common.entity import Entity


class User(Entity):
    """User aggregate root without command-level validation."""

    def __init__(
        self,
        name: str,
        email: str,
        password: str,
        *,
        refresh_token_hash: str | None = None,
        refresh_token_expires_at: datetime | None = None,
    ) -> None:
        super().__init__()
        self._name = name
        self._email = email
        self._password = password
        self._refresh_token_hash = refresh_token_hash
        self._refresh_token_expires_at = refresh_token_expires_at

    @classmethod
    def from_persistence(
        cls,
        *,
        id: int,
        name: str,
        email: str,
        password: str,
        refresh_token_hash: str | None = None,
        refresh_token_expires_at: datetime | None = None,
    ) -> User:
        user = cls.__new__(cls)
        Entity.__init__(user)
        user.id = id
        user._name = name
        user._email = email
        user._password = password
        user._refresh_token_hash = refresh_token_hash
        user._refresh_token_expires_at = refresh_token_expires_at
        return user

    @property
    def name(self) -> str:
        return self._name

    @property
    def email(self) -> str:
        return self._email

    @property
    def password(self) -> str:
        return self._password

    @property
    def refresh_token_hash(self) -> str | None:
        return self._refresh_token_hash

    @property
    def refresh_token_expires_at(self) -> datetime | None:
        return self._refresh_token_expires_at

    def set_name(self, name: str) -> None:
        self._name = name

    def set_email(self, email: str) -> None:
        self._email = email

    def set_password(self, password: str) -> None:
        self._password = password

    def set_refresh_token(
        self,
        refresh_token_hash: str | None,
        expires_at: datetime | None,
    ) -> None:
        self._refresh_token_hash = refresh_token_hash
        self._refresh_token_expires_at = expires_at

    def clear_refresh_token(self) -> None:
        self._refresh_token_hash = None
        self._refresh_token_expires_at = None
