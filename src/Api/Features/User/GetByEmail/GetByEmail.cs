using System.Reflection.Metadata;
using Api.Extensions;
using Api.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Features.User.GetByEmail;

public class GetByEmail : BaseApi, IEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapGet("/api/user/{email}", Handle)
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle([FromQuery] string email, ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetByEmailQuery(email), cancellationToken);

        return result.Match(
            Results.Ok,
            Problem);
    }
}