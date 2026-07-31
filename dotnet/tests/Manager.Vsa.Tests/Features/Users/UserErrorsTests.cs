using FluentAssertions;
using Manager.Api.Features.Users;

namespace Manager.Vsa.Tests.Features.Users;

public class UserErrorsTests
{
    [Fact]
    public void NotFound_HasExpectedCode()
    {
        var error = UserErrors.NotFound();
        error.Code.Should().Be("Users.NotFound");
        error.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void EmailConflict_HasExpectedCode()
    {
        var error = UserErrors.EmailConflict();
        error.Code.Should().Be("Users.EmailConflict");
        error.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void BootstrapNotAllowed_HasExpectedCode()
    {
        var error = UserErrors.BootstrapNotAllowed();
        error.Code.Should().Be("Users.BootstrapNotAllowed");
        error.Description.Should().NotBeNullOrWhiteSpace();
    }
}
