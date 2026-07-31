using Manager.Api.Common;
using Manager.Api.Common.Messaging;
using Manager.Api.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Manager.Api.Features.Users;

public static class GetUser
{
    public sealed record Query(long Id) : IQuery<Response>;

    public sealed record Response(long Id, string Name, string Email);

    internal sealed class Handler(
        ApplicationDbContext db,
        HybridCache cache) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken cancellationToken)
        {
            var response = await cache.GetOrCreateAsync(
                UserCacheKeys.ById(query.Id),
                async ct =>
                {
                    return await db.Users
                        .AsNoTracking()
                        .Where(u => u.Id == query.Id)
                        .Select(u => new Response(u.Id, u.Name, u.Email))
                        .SingleOrDefaultAsync(ct);
                },
                cancellationToken: cancellationToken);

            if (response is null)
            {
                await cache.RemoveAsync(UserCacheKeys.ById(query.Id), cancellationToken);
                return Result.Failure<Response>(UserErrors.NotFound());
            }

            return Result.Success(response);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app) =>
            app.MapGet(
                    "/api/v1/users/{id:long}",
                    async (
                        long id,
                        [FromServices] IQueryHandler<Query, Response> handler,
                        CancellationToken cancellationToken) =>
                    {
                        var result = await handler.Handle(new Query(id), cancellationToken);
                        return result.Match(Results.Ok, CustomResults.Problem);
                    })
                .RequireAuthorization()
                .WithTags(Tags.Users);
    }
}
