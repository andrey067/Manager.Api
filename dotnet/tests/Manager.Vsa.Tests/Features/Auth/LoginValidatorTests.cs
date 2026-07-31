using FluentValidation.TestHelper;
using Manager.Api.Features.Auth;

namespace Manager.Vsa.Tests.Features.Auth;

public class LoginValidatorTests
{
    private readonly Login.Validator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Login_Is_Empty()
    {
        var result = _validator.TestValidate(new Login.Command("", "password"));
        result.ShouldHaveValidationErrorFor(x => x.Login);
    }

    [Fact]
    public void Should_Have_Error_When_Password_Is_Empty()
    {
        var result = _validator.TestValidate(new Login.Command("user@example.com", ""));
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_Not_Have_Errors_When_Command_Is_Valid()
    {
        var result = _validator.TestValidate(new Login.Command("user@example.com", "password"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
