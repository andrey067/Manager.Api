using FluentAssertions;
using Manager.Api.Authentication;

namespace Manager.Vsa.Tests.Authentication;

public class AuthErrorsTests
{
    [Fact]
    public void InvalidCredentials_HasExpectedCode()
    {
        var error = AuthErrors.InvalidCredentials();
        error.Code.Should().Be("Auth.InvalidCredentials");
        error.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void InvalidRefreshToken_HasExpectedCode()
    {
        var error = AuthErrors.InvalidRefreshToken();
        error.Code.Should().Be("Auth.InvalidRefreshToken");
        error.Description.Should().NotBeNullOrWhiteSpace();
    }
}
