using Api.Extensions;
using Api.Interfaces;
using MediatR;

namespace Api.Features.User.GetAll;

public class GetAllUser : BaseApi, IEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapGet("/api/users", Handler)
            .RequireAuthorization();
    }

    private static async Task<IResult> Handler(ISender sender, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new GetAllUserQuery(), cancellationToken);
        
        return response.Match(
            Results.Ok,
            Problem);
    }
}