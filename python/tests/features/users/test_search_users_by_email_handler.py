from __future__ import annotations

import pytest
from sqlalchemy.ext.asyncio import AsyncSession

from authentication.password_hasher import Argon2PasswordHasher
from features.users.search_users_by_email import Handler, Query
from tests.features.conftest import seed_user


@pytest.mark.asyncio
async def test_handler_returns_matching_users_case_insensitive(
    vsa_session: AsyncSession,
    password_hasher: Argon2PasswordHasher,
) -> None:
    await seed_user(
        vsa_session,
        password_hasher,
        email="one@example.com",
        name="User One",
    )
    await seed_user(
        vsa_session,
        password_hasher,
        email="two@EXAMPLE.com",
        name="User Two",
    )
    await seed_user(
        vsa_session,
        password_hasher,
        email="other@test.com",
        name="Other",
    )
    handler = Handler(vsa_session)

    result = await handler.handle(Query(email="example.com"))

    assert result.is_success
    assert len(result.value) == 2
    emails = {user.email for user in result.value}
    assert emails == {"one@example.com", "two@EXAMPLE.com"}


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

    result = await handler.handle(Query(email="none@example.com"))

    assert result.is_success
    assert result.value == []
