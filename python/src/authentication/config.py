from __future__ import annotations

import os
from dataclasses import dataclass


def _env_int(key: str, default: int) -> int:
    raw = os.getenv(key)
    if raw is None:
        return default
    try:
        return int(raw)
    except ValueError:
        return default


@dataclass(frozen=True, slots=True)
class HashSettings:
    time_cost: int
    parallelism: int
    memory_cost: int
    hash_len: int


@dataclass(frozen=True, slots=True)
class JwtSettings:
    secret: str
    issuer: str
    audience: str
    hours_to_expire: int
    refresh_days_to_expire: int
    algorithm: str = "HS256"


def get_hash_settings() -> HashSettings:
    return HashSettings(
        time_cost=_env_int("HASH_TIME_COST", 10),
        parallelism=_env_int("HASH_LANES", 5),
        memory_cost=_env_int("HASH_MEMORY_COST", 32768),
        hash_len=_env_int("HASH_LENGTH", 32),
    )


def get_jwt_settings() -> JwtSettings:
    secret = os.getenv("JWT_SECRET", "").strip()
    if len(secret) < 32:
        raise RuntimeError("JWT_SECRET must be configured with at least 32 characters.")
    return JwtSettings(
        secret=secret,
        issuer=os.getenv("JWT_ISSUER", "Manager.Api"),
        audience=os.getenv("JWT_AUDIENCE", "Manager.Api"),
        hours_to_expire=_env_int("JWT_HOURS_TO_EXPIRE", 1),
        refresh_days_to_expire=_env_int("JWT_REFRESH_DAYS_TO_EXPIRE", 7),
        algorithm=os.getenv("JWT_ALGORITHM", "HS256"),
    )
