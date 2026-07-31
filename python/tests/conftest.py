from __future__ import annotations

import importlib
from typing import Any, cast

import pytest


@pytest.fixture()
def fake() -> Any:
    faker_module = importlib.import_module("faker")
    faker_class = cast(Any, faker_module.Faker)

    f = faker_class("pt_BR")
    f.seed_instance(1234)
    return f


@pytest.fixture(autouse=True)
def _test_env(monkeypatch: pytest.MonkeyPatch, tmp_path) -> None:
    """Ensure required env vars exist for config/JWT during tests."""
    monkeypatch.setenv(
        "DATABASE_URL",
        "postgresql+asyncpg://test:test@localhost:5432/test_db",
    )
    monkeypatch.setenv("JWT_SECRET", "test-secret-key-for-unit-tests-only-32b")
    monkeypatch.setenv("JWT_ISSUER", "Manager.API")
    monkeypatch.setenv("JWT_AUDIENCE", "Manager.API")
    monkeypatch.setenv("JWT_HOURS_TO_EXPIRE", "1")
    monkeypatch.setenv("HASH_TIME_COST", "1")
    monkeypatch.setenv("HASH_LANES", "1")
    monkeypatch.setenv("HASH_MEMORY_COST", "8")
    monkeypatch.setenv("HASH_LENGTH", "16")
