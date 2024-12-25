using Api.Extensions;
using Api.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Features.User.Update;

public record struct UpdateUserRequest(string FirstName, string LastName, string Email, string Password);

public class UpdateUserValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.LastName).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class UpdateUser : BaseApi, IEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapPut("/api/v1/users/{id}", Handler)
            .RequireAuthorization();
    }

    private static async Task<IResult> Handler(long id, [FromBody] UpdateUserRequest updateUserRequest,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var updateUserCommand = new UpdateUserCommand(id, updateUserRequest.FirstName,
            updateUserRequest.LastName,
            updateUserRequest.Email,
            updateUserRequest.Password);

        var result = await sender.Send(updateUserCommand, cancellationToken);

        return result.Match(
            Results.Ok,
            Problem);
    }
}