from __future__ import annotations

import base64
import hashlib
import secrets
from datetime import UTC, datetime, timedelta

import jwt

from authentication.config import JwtSettings
from common.clock import Clock, SystemClock


class JwtTokenService:
    def __init__(
        self,
        settings: JwtSettings,
        clock: Clock | None = None,
    ) -> None:
        self._settings = settings
        self._clock = clock or SystemClock()

    def create_access_token(
        self, user_id: int, email: str
    ) -> tuple[str, datetime]:
        expires = self.get_access_expiry()
        payload = {
            "sub": str(user_id),
            "email": email,
            "role": "User",
            "iss": self._settings.issuer,
            "aud": self._settings.audience,
            "exp": expires,
            "iat": self._clock.utc_now(),
        }
        encoded = jwt.encode(
            payload,
            self._settings.secret,
            algorithm=self._settings.algorithm,
        )
        token = encoded.decode("utf-8") if isinstance(encoded, bytes) else str(encoded)
        return token, expires

    def create_refresh_token(self) -> str:
        return base64.b64encode(secrets.token_bytes(64)).decode("ascii")

    def hash_refresh_token(self, refresh_token: str) -> str:
        return hashlib.sha256(refresh_token.encode("utf-8")).hexdigest().upper()

    def get_access_expiry(self) -> datetime:
        return self._clock.utc_now() + timedelta(hours=self._settings.hours_to_expire)

    def get_refresh_expiry(self) -> datetime:
        return self._clock.utc_now() + timedelta(
            days=self._settings.refresh_days_to_expire
        )

    def validate_access_token(self, token: str) -> int | None:
        try:
            payload = jwt.decode(
                token,
                self._settings.secret,
                algorithms=[self._settings.algorithm],
                audience=self._settings.audience,
                issuer=self._settings.issuer,
                options={"verify_exp": False},
            )
            exp = payload.get("exp")
            if exp is not None:
                exp_dt = exp if isinstance(exp, datetime) else datetime.fromtimestamp(
                    int(exp), UTC
                )
                if exp_dt.tzinfo is None:
                    exp_dt = exp_dt.replace(tzinfo=UTC)
                if exp_dt < self._clock.utc_now():
                    return None
            subject = payload.get("sub")
            if subject is None:
                return None
            return int(subject)
        except (jwt.PyJWTError, ValueError, TypeError):
            return None
