from __future__ import annotations

import pytest
from sqlalchemy.ext.asyncio import AsyncSession

from authentication.password_hasher import Argon2PasswordHasher
from features.users.search_users_by_name import Handler, Query
from tests.features.conftest import seed_user


@pytest.mark.asyncio
async def test_handler_returns_matching_users_case_insensitive(
    vsa_session: AsyncSession,
    password_hasher: Argon2PasswordHasher,
) -> None:
    await seed_user(
        vsa_session,
        password_hasher,
        email="alice@example.com",
        name="Alice Smith",
    )
    await seed_user(
        vsa_session,
        password_hasher,
        email="bob@example.com",
        name="Bob Alice",
    )
    await seed_user(
        vsa_session,
        password_hasher,
        email="charlie@example.com",
        name="Charlie",
    )
    handler = Handler(vsa_session)

    result = await handler.handle(Query(name="alice"))

    assert result.is_success
    assert len(result.value) == 2
    names = {user.name for user in result.value}
    assert names == {"Alice Smith", "Bob Alice"}


@pytest.mark.asyncio
async def test_handler_returns_empty_list_when_no_match(
    vsa_session: AsyncSession,
    password_hasher: Argon2PasswordHasher,
) -> None:
    await seed_user(
        vsa_session,
        password_hasher,
        email="alice@example.com",
        name="Alice",
    )
    handler = Handler(vsa_session)

    result = await handler.handle(Query(name="nobody"))

    assert result.is_success
    assert result.value == []
