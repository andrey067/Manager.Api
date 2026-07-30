from __future__ import annotations

from common.error import Error


class AuthErrors:
    @staticmethod
    def invalid_credentials() -> Error:
        return Error.problem(
            "Auth.InvalidCredentials",
            "Invalid email or password.",
        )

    @staticmethod
    def invalid_refresh_token() -> Error:
        return Error.problem(
            "Auth.InvalidRefreshToken",
            "Invalid or expired refresh token.",
        )
