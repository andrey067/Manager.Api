using System.Reflection.Metadata;
using Api.Extensions;
using Api.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Features.User.SearchByName;

public record struct SearchByNameRequest(string Name);

public class SearchByName : BaseApi, IEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapGet("/api/user/search/{email}", Handle)
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle([FromQuery] string email, ISender sender,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(new SearchByNameQuery(email), cancellationToken);
        return response.Match(
            Results.Ok,
            Problem);
    }
}