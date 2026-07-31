from __future__ import annotations

from authentication.errors import AuthErrors


def test_invalid_credentials_code() -> None:
    error = AuthErrors.invalid_credentials()
    assert error.code == "Auth.InvalidCredentials"
    assert error.description


def test_invalid_refresh_token_code() -> None:
    error = AuthErrors.invalid_refresh_token()
    assert error.code == "Auth.InvalidRefreshToken"
    assert error.description
