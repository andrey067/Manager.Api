using FluentValidation;
using Manager.Api.Authentication;
using Manager.Api.Common;
using Manager.Api.Common.Messaging;
using Manager.Api.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Manager.Api.Features.Users;

public static class UpdateUser
{
    public sealed record Command(long Id, string Name, string Email, string Password) : ICommand<Response>;

    public sealed record Request(string Name, string Email, string Password);

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
            var user = await db.Users
                .SingleOrDefaultAsync(u => u.Id == command.Id, cancellationToken);

            if (user is null)
                return Result.Failure<Response>(UserErrors.NotFound());

            if (await db.Users.AnyAsync(
                    u => u.Email == command.Email && u.Id != command.Id,
                    cancellationToken))
                return Result.Failure<Response>(UserErrors.EmailConflict());

            var oldEmail = user.Email;

            user.SetName(command.Name);
            user.SetEmail(command.Email);

            if (!hasher.Verify(command.Password, user.Password))
                user.SetPassword(hasher.Hash(command.Password));

            user.Raise(new UserUpdatedDomainEvent(user.Id));
            await db.SaveChangesAsync(cancellationToken);

            await cache.RemoveAsync(UserCacheKeys.ById(user.Id), cancellationToken);
            await cache.RemoveAsync(UserCacheKeys.ByEmail(oldEmail), cancellationToken);
            await cache.RemoveAsync(UserCacheKeys.ByEmail(command.Email), cancellationToken);
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
                        long id,
                        Request request,
                        [FromServices] ICommandHandler<Command, Response> handler,
                        CancellationToken cancellationToken) =>
                    {
                        var command = new Command(id, request.Name, request.Email, request.Password);
                        var result = await handler.Handle(command, cancellationToken);
                        return result.Match(Results.Ok, CustomResults.Problem);
                    })
                .AddEndpointFilter<ValidationEndpointFilter<Command>>()
                .RequireAuthorization()
                .WithTags(Tags.Users);
    }
}
