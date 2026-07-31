from __future__ import annotations

from argon2 import PasswordHasher as Argon2Hasher
from argon2.exceptions import VerifyMismatchError

from authentication.config import HashSettings


class Argon2PasswordHasher:
    def __init__(self, settings: HashSettings) -> None:
        self._hasher = Argon2Hasher(
            time_cost=settings.time_cost,
            memory_cost=settings.memory_cost,
            parallelism=settings.parallelism,
            hash_len=settings.hash_len,
        )

    def hash(self, plain: str) -> str:
        return self._hasher.hash(plain)

    def verify(self, plain: str, hashed: str) -> bool:
        try:
            return bool(self._hasher.verify(hashed, plain))
        except VerifyMismatchError:
            return False
        except Exception:
            return False
