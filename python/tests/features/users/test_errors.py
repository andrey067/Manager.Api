from __future__ import annotations

from features.users.errors import UserErrors


def test_not_found_code() -> None:
    error = UserErrors.not_found()
    assert error.code == "Users.NotFound"
    assert error.description


def test_email_conflict_code() -> None:
    error = UserErrors.email_conflict()
    assert error.code == "Users.EmailConflict"
    assert error.description


def test_bootstrap_not_allowed_code() -> None:
    error = UserErrors.bootstrap_not_allowed()
    assert error.code == "Users.BootstrapNotAllowed"
    assert error.description
