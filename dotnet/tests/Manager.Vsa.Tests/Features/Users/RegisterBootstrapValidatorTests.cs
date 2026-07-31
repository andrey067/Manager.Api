using FluentValidation.TestHelper;
using Manager.Api.Features.Users;

namespace Manager.Vsa.Tests.Features.Users;

public class RegisterBootstrapValidatorTests
{
    private readonly RegisterBootstrap.Validator _validator = new();

    private static RegisterBootstrap.Command ValidCommand() =>
        new("Valid Name", "user@example.com", "Password1!");

    [Fact]
    public void Should_Have_Error_When_Name_Too_Short()
    {
        var result = _validator.TestValidate(ValidCommand() with { Name = "A" });
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Should_Have_Error_When_Name_Too_Long()
    {
        var result = _validator.TestValidate(ValidCommand() with { Name = new string('a', 81) });
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Should_Have_Error_When_Email_Invalid()
    {
        var result = _validator.TestValidate(ValidCommand() with { Email = "not-an-email" });
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Should_Have_Error_When_Email_Too_Long()
    {
        var local = new string('a', 170);
        var result = _validator.TestValidate(ValidCommand() with { Email = $"{local}@example.com" });
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Should_Have_Error_When_Password_Too_Short()
    {
        var result = _validator.TestValidate(ValidCommand() with { Password = "short" });
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_Have_Error_When_Password_Too_Long()
    {
        var result = _validator.TestValidate(ValidCommand() with { Password = new string('a', 31) });
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_Not_Have_Errors_When_Command_Is_Valid()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }
}
