using FluentValidation;
using Manager.Api.Authentication;
using Manager.Api.Common;
using Manager.Api.Common.Messaging;
using Manager.Api.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Manager.Api.Features.Users;

public static class CreateUser
{
    public sealed record Command(string Name, string Email, string Password) : ICommand<Response>;

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

            if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
                return Result.Failure<Response>(UserErrors.EmailConflict());

            var user = User.Create(
                name,
                email,
                hasher.Hash(command.Password));

            db.Users.Add(user);
            await db.SaveChangesAsync(cancellationToken);

            user.Raise(new UserCreatedDomainEvent(user.Id));
            await cache.RemoveAsync(UserCacheKeys.All, cancellationToken);
            await cache.RemoveAsync(UserCacheKeys.ByEmail(email), cancellationToken);

            return Result.Success(new Response(user.Id, user.Name, user.Email));
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app) =>
            app.MapPost(
                    "/api/v1/users",
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
