using Api.Extensions;
using Api.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Features.User.SearchByEmail;

public class SearchByEmail : BaseApi, IEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapGet("/api/user/search-by-email/{email}", Handle);
    }

    private static async Task<IResult> Handle([FromQuery] string email, ISender sender,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(new SearchByEmailQuery(email), cancellationToken);
        return response.Match(
            Results.Ok,
            Problem
        );
    }
}