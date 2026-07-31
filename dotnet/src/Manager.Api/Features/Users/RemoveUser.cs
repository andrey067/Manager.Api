using Manager.Api.Common;
using Manager.Api.Common.Messaging;
using Manager.Api.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Manager.Api.Features.Users;

public static class RemoveUser
{
    public sealed record Command(long Id) : ICommand;

    internal sealed class Handler(
        ApplicationDbContext db,
        HybridCache cache) : ICommandHandler<Command>
    {
        public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
        {
            var user = await db.Users
                .SingleOrDefaultAsync(u => u.Id == command.Id, cancellationToken);

            if (user is null)
                return Result.Failure(UserErrors.NotFound());

            var email = user.Email;

            user.Raise(new UserRemovedDomainEvent(user.Id));
            db.Users.Remove(user);
            await db.SaveChangesAsync(cancellationToken);

            await cache.RemoveAsync(UserCacheKeys.ById(user.Id), cancellationToken);
            await cache.RemoveAsync(UserCacheKeys.ByEmail(email), cancellationToken);
            await cache.RemoveAsync(UserCacheKeys.All, cancellationToken);

            return Result.Success();
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app) =>
            app.MapDelete(
                    "/api/v1/users/{id:long}",
                    async (
                        long id,
                        [FromServices] ICommandHandler<Command> handler,
                        CancellationToken cancellationToken) =>
                    {
                        var result = await handler.Handle(new Command(id), cancellationToken);
                        return result.Match(Results.NoContent, CustomResults.Problem);
                    })
                .RequireAuthorization()
                .WithTags(Tags.Users);
    }
}
