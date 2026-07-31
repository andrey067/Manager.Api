from __future__ import annotations

import pytest

from authentication.config import HashSettings
from authentication.password_hasher import Argon2PasswordHasher


@pytest.fixture()
def hasher() -> Argon2PasswordHasher:
    return Argon2PasswordHasher(
        HashSettings(time_cost=1, parallelism=1, memory_cost=8, hash_len=16)
    )


def test_hash_and_verify_roundtrip(hasher: Argon2PasswordHasher) -> None:
    hashed = hasher.hash("Secret123!")
    assert hashed.startswith("$argon2")
    assert hasher.verify("Secret123!", hashed) is True
    assert hasher.verify("WrongPassword", hashed) is False
