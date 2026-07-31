using FluentValidation;
using Manager.Api.Authentication;
using Manager.Api.Common;
using Manager.Api.Common.Messaging;
using Manager.Api.Database;
using Manager.Api.Features.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Manager.Api.Features.Auth;

public static class Login
{
    public sealed record Command(string Login, string Password) : ICommand<Response>;

    public sealed record Response(
        string AccessToken,
        DateTime AccessTokenExpires,
        string RefreshToken,
        DateTime RefreshTokenExpires);

    internal sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Login).NotEmpty();
            RuleFor(c => c.Password).NotEmpty();
        }
    }

    internal sealed class Handler(
        ApplicationDbContext db,
        IPasswordHasher hasher,
        ITokenService tokens) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command command, CancellationToken cancellationToken)
        {
            var login = command.Login.Trim();

            var user = await db.Users
                .FirstOrDefaultAsync(u => u.Email == login, cancellationToken);

            if (user is null || !hasher.Verify(command.Password, user.Password))
                return Result.Failure<Response>(AuthErrors.InvalidCredentials());

            var access = tokens.CreateAccessToken(user.Id, user.Email);
            var refresh = tokens.CreateRefreshToken();
            var refreshExpires = tokens.GetRefreshExpiry();

            user.SetRefreshToken(tokens.HashRefreshToken(refresh), refreshExpires);
            user.Raise(new UserLoggedInDomainEvent(user.Id));
            await db.SaveChangesAsync(cancellationToken);

            return Result.Success(new Response(
                access.Token,
                access.Expires,
                refresh,
                refreshExpires));
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app) =>
            app.MapPost(
                    "/api/v1/auth/login",
                    async (
                        Command command,
                        [FromServices] ICommandHandler<Command, Response> handler,
                        CancellationToken cancellationToken) =>
                    {
                        var result = await handler.Handle(command, cancellationToken);
                        return result.Match(Results.Ok, CustomResults.Problem);
                    })
                .AddEndpointFilter<ValidationEndpointFilter<Command>>()
                .AllowAnonymous()
                .WithTags(Tags.Auth);
    }
}
