using Manager.Api.Common;
using Manager.Api.Common.Messaging;
using Manager.Api.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Manager.Api.Features.Users;

public static class SearchUsersByEmail
{
    public sealed record Query(string Email) : IQuery<List<Response>>;

    public sealed record Response(long Id, string Name, string Email);

    internal sealed class Handler(ApplicationDbContext db) : IQueryHandler<Query, List<Response>>
    {
        public async Task<Result<List<Response>>> Handle(Query query, CancellationToken cancellationToken)
        {
            var users = await db.Users
                .AsNoTracking()
                .Where(u => u.Email.ToLower().Contains(query.Email.ToLower()))
                .OrderBy(u => u.Id)
                .Select(u => new Response(u.Id, u.Name, u.Email))
                .ToListAsync(cancellationToken);

            return Result.Success(users);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app) =>
            app.MapGet(
                    "/api/v1/users/search-by-email",
                    async (
                        [FromQuery] string email,
                        [FromServices] IQueryHandler<Query, List<Response>> handler,
                        CancellationToken cancellationToken) =>
                    {
                        var result = await handler.Handle(new Query(email), cancellationToken);
                        return result.Match(Results.Ok, CustomResults.Problem);
                    })
                .RequireAuthorization()
                .WithTags(Tags.Users);
    }
}
