using Manager.Api.Common;

namespace Manager.Api.Authentication;

public static class AuthErrors
{
    public static Error InvalidCredentials()
        => Error.Problem("Auth.InvalidCredentials", "Invalid email or password.");

    public static Error InvalidRefreshToken()
        => Error.Problem("Auth.InvalidRefreshToken", "Invalid or expired refresh token.");
}
