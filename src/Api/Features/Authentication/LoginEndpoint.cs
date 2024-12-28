using Api.Extensions;
using Api.Interfaces;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Features.Authentication;

public sealed record LoginRequest(string Login, string Password);

public sealed record LoginResponse(string Token, DateTime TokenExpires);

public sealed record LoginCommand(string Login, string Password) : IRequest<ErrorOr<LoginResponse>>;

public class LoginEndpoint : BaseApi, IEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapPost("/login", Handler)
            .AllowAnonymous();
    }

    private static async Task<IResult> Handler([FromBody] LoginRequest request,
        IValidator<LoginRequest> validator,
        ISender sender, CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return Results.BadRequest(validationResult.Errors);

        var command = new LoginCommand(request.Login, request.Password);
        var response = await sender.Send(command, cancellationToken);

        return response.Match(
            Results.Ok,
            Problem);
    }
}