from __future__ import annotations

from collections.abc import AsyncGenerator
from datetime import UTC, datetime

import pytest
from sqlalchemy import event
from sqlalchemy.ext.asyncio import (
    AsyncEngine,
    AsyncSession,
    async_sessionmaker,
    create_async_engine,
)
from sqlalchemy.pool import StaticPool

from authentication.config import JwtSettings
from authentication.password_hasher import Argon2PasswordHasher
from authentication.token_service import JwtTokenService
from database.models import UserModel
from database.session import Base


class FixedClock:
    def __init__(self, moment: datetime | None = None) -> None:
        self._moment = moment or datetime(2026, 7, 30, 12, 0, 0, tzinfo=UTC)

    def utc_now(self) -> datetime:
        return self._moment


@pytest.fixture()
def fixed_clock() -> FixedClock:
    return FixedClock()


@pytest.fixture()
async def vsa_engine() -> AsyncGenerator[AsyncEngine, None]:
    engine = create_async_engine(
        "sqlite+aiosqlite://",
        connect_args={"check_same_thread": False},
        poolclass=StaticPool,
    )

    @event.listens_for(engine.sync_engine, "connect")
    def _fk_on(dbapi_conn, _connection_record) -> None:  # type: ignore[no-untyped-def]
        cursor = dbapi_conn.cursor()
        cursor.execute("PRAGMA foreign_keys=ON")
        cursor.close()

    async with engine.begin() as conn:
        await conn.run_sync(Base.metadata.create_all)

    try:
        yield engine
    finally:
        await engine.dispose()


@pytest.fixture()
async def vsa_session(vsa_engine: AsyncEngine) -> AsyncGenerator[AsyncSession, None]:
    factory = async_sessionmaker(
        vsa_engine,
        class_=AsyncSession,
        autocommit=False,
        autoflush=False,
        expire_on_commit=False,
    )
    async with factory() as session:
        yield session
        await session.rollback()


@pytest.fixture()
def password_hasher() -> Argon2PasswordHasher:
    from authentication.config import get_hash_settings

    return Argon2PasswordHasher(get_hash_settings())


@pytest.fixture()
def token_service(fixed_clock: FixedClock) -> JwtTokenService:
    settings = JwtSettings(
        secret="test-secret-key-for-unit-tests-only-32b",
        issuer="Manager.Api",
        audience="Manager.Api",
        hours_to_expire=1,
        refresh_days_to_expire=7,
    )
    return JwtTokenService(settings, clock=fixed_clock)


async def seed_user(
    session: AsyncSession,
    hasher: Argon2PasswordHasher,
    *,
    email: str = "user@example.com",
    password: str = "Secret123!",
    name: str = "Test User",
) -> UserModel:
    user = UserModel(
        name=name,
        email=email,
        password=hasher.hash(password),
    )
    session.add(user)
    await session.commit()
    await session.refresh(user)
    return user


async def seed_user_with_refresh_token(
    session: AsyncSession,
    token_service: JwtTokenService,
    *,
    email: str = "user@example.com",
    password: str = "hash",
    name: str = "Test User",
    refresh_token: str = "valid-refresh-token",
    expires_at: datetime | None = None,
) -> UserModel:
    if expires_at is None:
        expires_at = token_service.get_refresh_expiry()
    stored_expires = (
        expires_at.replace(tzinfo=None) if expires_at.tzinfo is not None else expires_at
    )
    user = UserModel(
        name=name,
        email=email,
        password=password,
        refresh_token_hash=token_service.hash_refresh_token(refresh_token),
        refresh_token_expires_at=stored_expires,
    )
    session.add(user)
    await session.commit()
    await session.refresh(user)
    return user
