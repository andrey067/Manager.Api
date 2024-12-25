using Api.Database;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.User.Remove;

public record struct RemoveUserQuery(long Id) : IRequest<ErrorOr<Unit>>;

public class RemoveUserHandler(ILogger<RemoveUserHandler> logger, ManagerContext context)
    : IRequestHandler<RemoveUserQuery, ErrorOr<Unit>>
{
    public async Task<ErrorOr<Unit>> Handle(RemoveUserQuery request, CancellationToken cancellationToken)
    {
        var user = await context.Users.SingleOrDefaultAsync(x => x.Id.Value == request.Id, cancellationToken);
        if (user is null)
        {
            logger.LogWarning("User with ID {UserId} not found", request.Id);
            return Error.NotFound($"User with ID '{request.Id}' not found");
        }

        context.Users.Remove(user);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User with ID {UserId} was removed", request.Id);
        return Unit.Value;
    }
}