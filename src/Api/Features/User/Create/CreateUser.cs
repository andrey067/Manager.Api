using Api.Extensions;
using Api.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Features.User.Create;

public record struct CreateUserRequest(string FirstName, string LastName, string Email, string Password);

public record struct CreateUserResponse(long Id, string Name, string Email);

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().WithMessage("Name is required.");
        RuleFor(x => x.LastName).NotEmpty().WithMessage("Name is required.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("A valid email is required.");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6)
            .WithMessage("Password must be at least 6 characters long.");
    }
}

public class CreateUser : BaseApi, IEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapPost("/api/v1/users", Handler)
            .RequireAuthorization();
    }

    private static async Task<IResult> Handler([FromBody] CreateUserRequest createUserRequest,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var validator = new CreateUserRequestValidator();
        var validationResult = await validator.ValidateAsync(createUserRequest, cancellationToken);
        if (!validationResult.IsValid)
            return Results.BadRequest(validationResult.Errors);

        var command = new CreateUserCommand(createUserRequest.FirstName,
            createUserRequest.LastName,
            createUserRequest.Email,
            createUserRequest.Password);

        var result = await sender.Send(command, cancellationToken);

        return result.Match(
            Results.Ok,
            Problem);
    }
}