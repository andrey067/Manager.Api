using Manager.Api.Common;
using Manager.Api.Common.Messaging;
using Manager.Api.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Manager.Api.Features.Users;

public static class GetUserByEmail
{
    public sealed record Query(string Email) : IQuery<Response>;

    public sealed record Response(long Id, string Name, string Email);

    internal sealed class Handler(
        ApplicationDbContext db,
        HybridCache cache) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken cancellationToken)
        {
            var response = await cache.GetOrCreateAsync(
                UserCacheKeys.ByEmail(query.Email),
                async ct =>
                {
                    return await db.Users
                        .AsNoTracking()
                        .Where(u => u.Email.ToLower() == query.Email.ToLower())
                        .Select(u => new Response(u.Id, u.Name, u.Email))
                        .SingleOrDefaultAsync(ct);
                },
                cancellationToken: cancellationToken);

            if (response is null)
            {
                await cache.RemoveAsync(UserCacheKeys.ByEmail(query.Email), cancellationToken);
                return Result.Failure<Response>(UserErrors.NotFound());
            }

            return Result.Success(response);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app) =>
            app.MapGet(
                    "/api/v1/users/by-email",
                    async (
                        [FromQuery] string email,
                        [FromServices] IQueryHandler<Query, Response> handler,
                        CancellationToken cancellationToken) =>
                    {
                        var result = await handler.Handle(new Query(email), cancellationToken);
                        return result.Match(Results.Ok, CustomResults.Problem);
                    })
                .RequireAuthorization()
                .WithTags(Tags.Users);
    }
}
