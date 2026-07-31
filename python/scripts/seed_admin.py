"""Seed the first admin user (bootstrap via CLI).

Usage (from python/):
  uv run python scripts/seed_admin.py
  # or with overrides:
  SEED_NAME="Admin" SEED_EMAIL="admin@example.com" SEED_PASSWORD="Secret123" \\
    uv run python scripts/seed_admin.py

Requires DATABASE_URL and HASH_* in .env. Fails if any user already exists.
"""

from __future__ import annotations

import asyncio
import os
import sys
from pathlib import Path

from dotenv import load_dotenv

_ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(_ROOT / "src"))
load_dotenv(_ROOT / ".env")

from authentication.config import get_hash_settings  # noqa: E402
from authentication.password_hasher import Argon2PasswordHasher  # noqa: E402
from common.cache import AppCache  # noqa: E402
from database.session import get_session_factory  # noqa: E402
from features.users.register_bootstrap import Command, Handler  # noqa: E402


async def main() -> int:
    name = os.getenv("SEED_NAME", "Admin User")
    email = os.getenv("SEED_EMAIL", "admin@example.com")
    password = os.getenv("SEED_PASSWORD", "ChangeMe123")

    factory = get_session_factory()
    async with factory() as session:
        handler = Handler(
            session=session,
            password_hasher=Argon2PasswordHasher(get_hash_settings()),
            cache=AppCache(),
        )
        result = await handler.handle(
            Command(name=name, email=email, password=password)
        )
        if result.is_failure:
            print("Bootstrap failed:", result.error.description)
            return 1
        value = result.value
        print(f"Created user id={value.id} email={value.email}")
        return 0


if __name__ == "__main__":
    raise SystemExit(asyncio.run(main()))
