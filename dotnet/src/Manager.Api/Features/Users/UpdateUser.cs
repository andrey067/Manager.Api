using System.Reflection;
using FluentValidation;
using Manager.Api.Authentication;
using Manager.Api.Common;
using Manager.Api.Common.Messaging;
using Manager.Api.Database;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Manager.Api.Features.Users;

public static class UpdateUser
{
    public sealed record Command(long Id, string Name, string Email, string Password) : ICommand<Response>
    {
        public static async ValueTask<Command?> BindAsync(HttpContext httpContext, ParameterInfo parameter)
        {
            if (!long.TryParse(httpContext.Request.RouteValues["id"]?.ToString(), out var id))
                return null;

            var body = await httpContext.Request.ReadFromJsonAsync<Body>();
            if (body is null)
                return null;

            return new Command(id, body.Name, body.Email, body.Password);
        }

        private sealed record Body(string Name, string Email, string Password);
    }

    public sealed record Response(long Id, string Name, string Email);

    internal sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Name)
                .NotEmpty()
                .MinimumLength(2)
                .MaximumLength(80);

            RuleFor(c => c.Email)
                .NotEmpty()
                .MaximumLength(180)
                .EmailAddress();

            RuleFor(c => c.Password)
                .NotEmpty()
                .MinimumLength(8)
                .MaximumLength(30);
        }
    }

    internal sealed class Handler(
        ApplicationDbContext db,
        IPasswordHasher hasher,
        HybridCache cache) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command command, CancellationToken cancellationToken)
        {
            var name = command.Name.Trim();
            var email = command.Email.Trim();

            var user = await db.Users
                .SingleOrDefaultAsync(u => u.Id == command.Id, cancellationToken);

            if (user is null)
                return Result.Failure<Response>(UserErrors.NotFound());

            if (await db.Users.AnyAsync(
                    u => u.Email == email && u.Id != command.Id,
                    cancellationToken))
                return Result.Failure<Response>(UserErrors.EmailConflict());

            var oldEmail = user.Email;

            user.SetName(name);
            user.SetEmail(email);

            if (!hasher.Verify(command.Password, user.Password))
                user.SetPassword(hasher.Hash(command.Password));

            user.Raise(new UserUpdatedDomainEvent(user.Id));
            await db.SaveChangesAsync(cancellationToken);

            await cache.RemoveAsync(UserCacheKeys.ById(user.Id), cancellationToken);
            await cache.RemoveAsync(UserCacheKeys.ByEmail(oldEmail), cancellationToken);
            await cache.RemoveAsync(UserCacheKeys.ByEmail(email), cancellationToken);
            await cache.RemoveAsync(UserCacheKeys.All, cancellationToken);

            return Result.Success(new Response(user.Id, user.Name, user.Email));
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app) =>
            app.MapPut(
                    "/api/v1/users/{id:long}",
                    async (
                        Command command,
                        [FromServices] ICommandHandler<Command, Response> handler,
                        CancellationToken cancellationToken) =>
                    {
                        var result = await handler.Handle(command, cancellationToken);
                        return result.Match(Results.Ok, CustomResults.Problem);
                    })
                .AddEndpointFilter<ValidationEndpointFilter<Command>>()
                .RequireAuthorization()
                .WithTags(Tags.Users);
    }
}
