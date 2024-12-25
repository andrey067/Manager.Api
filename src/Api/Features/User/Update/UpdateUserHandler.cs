using Api.Database;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.User.Update;

public record struct UpdateUserResponse(long Id, string Name, string Email);

public record struct UpdateUserCommand(long Id, string FirstName, string LastName, string Email, string Password)
    : IRequest<ErrorOr<UpdateUserResponse>>;

public class UpdateUserHandler(ILogger<UpdateUserHandler> logger, ManagerContext context)
    : IRequestHandler<UpdateUserCommand, ErrorOr<UpdateUserResponse>>
{
    public async Task<ErrorOr<UpdateUserResponse>> Handle(UpdateUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await context.Users.Include(user => user.Id)
            .SingleOrDefaultAsync(u => u.Id.Value == request.Id, cancellationToken);

        if (user is null)
        {
            logger.LogInformation("User with email '{Email}' not found", request.Email);
            return Error.NotFound($"User with email '{request.Email}' not found");
        }

        user.Update(request.FirstName, request.LastName, request.Email, request.Password);

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Updated user: {@User}", user);

        var response = new UpdateUserResponse(user.Id.Value, user.ToString()!, user.Email);
        return response;
    }
}