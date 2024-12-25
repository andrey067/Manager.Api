using Api.Extensions;
using Api.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Features.User.Remove;

public class RemoveUser : BaseApi, IEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapDelete("/api/user/{id}", Handler)
            .RequireAuthorization();
    }

    private static async Task<IResult> Handler([FromQuery] long id, ISender sender, CancellationToken cancellationToken)
    {
        var command = new RemoveUserQuery(id);
        var result = await sender.Send(command, cancellationToken);
        return result.Match(
            Results.Ok,
            Problem);
    }
}