using Manager.Api.Common;
using Manager.Api.Common.Messaging;
using Manager.Api.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Manager.Api.Features.Users;

public static class GetAllUsers
{
    public sealed record Query : IQuery<List<Response>>;

    public sealed record Response(long Id, string Name, string Email);

    internal sealed class Handler(
        ApplicationDbContext db,
        HybridCache cache) : IQueryHandler<Query, List<Response>>
    {
        public async Task<Result<List<Response>>> Handle(Query query, CancellationToken cancellationToken)
        {
            var users = await cache.GetOrCreateAsync(
                UserCacheKeys.All,
                async ct => await db.Users
                    .AsNoTracking()
                    .OrderBy(u => u.Id)
                    .Select(u => new Response(u.Id, u.Name, u.Email))
                    .ToListAsync(ct),
                cancellationToken: cancellationToken);

            return Result.Success(users);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app) =>
            app.MapGet(
                    "/api/v1/users",
                    async (
                        [FromServices] IQueryHandler<Query, List<Response>> handler,
                        CancellationToken cancellationToken) =>
                    {
                        var result = await handler.Handle(new Query(), cancellationToken);
                        return result.Match(Results.Ok, CustomResults.Problem);
                    })
                .RequireAuthorization()
                .WithTags(Tags.Users);
    }
}
