using Api.Extensions;
using Api.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Features.User.Get;

public class GetUser : BaseApi, IEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapGet("/api/user/{id}", Handler)
            .RequireAuthorization();
    }

    private static async Task<IResult> Handler([FromQuery] long Id, ISender sender, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new GetUserQuery(Id), cancellationToken);
        return response.Match(
            _ => Results.Created($"/api/user/{Id}", new { Id }),
            error => Problem(error)
        );
    }
}