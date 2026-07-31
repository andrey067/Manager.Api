using FluentValidation.TestHelper;
using Manager.Api.Features.Auth;

namespace Manager.Vsa.Tests.Features.Auth;

public class RefreshTokenValidatorTests
{
    private readonly RefreshToken.Validator _validator = new();

    [Fact]
    public void Should_Have_Error_When_RefreshToken_Is_Empty()
    {
        var result = _validator.TestValidate(new RefreshToken.Command(""));
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken);
    }

    [Fact]
    public void Should_Not_Have_Errors_When_Command_Is_Valid()
    {
        var result = _validator.TestValidate(new RefreshToken.Command("valid-refresh-token"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
