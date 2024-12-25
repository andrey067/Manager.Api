using Api.Database;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.User.Get;

public record struct GetUserQuery(long Id) : IRequest<ErrorOr<Unit>>;

public class GetUserHandler(ILogger<GetUserQuery> logger, ManagerContext context)
    : IRequestHandler<GetUserQuery, ErrorOr<Unit>>
{
    public async Task<ErrorOr<Unit>> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await context.Users.SingleOrDefaultAsync(x => x.Id.Value == request.Id, cancellationToken);
        if (user is not null) return Error.NotFound($"User with ID '{request.Id}' not found");

        logger.LogWarning("User with ID {UserId} not found", request.Id);
        return Error.NotFound($"User with ID '{request.Id}' not found");
    }
}