using Api.Database;
using Api.Features.User.GetAll;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.User.GetByEmail;

internal record struct GetByEmailQuery(string Email) : IRequest<ErrorOr<UserResponse>>;

internal class GetByEmailHandler(ILogger<GetByEmailHandler> logger, ManagerContext context)
    : IRequestHandler<GetByEmailQuery, ErrorOr<UserResponse>>
{
    public async Task<ErrorOr<UserResponse>> Handle(GetByEmailQuery request, CancellationToken cancellationToken)
    {
        var user = await context.Users.SingleOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user is not null)
            return new UserResponse(user.Name.ToString(), user.Email);

        logger.LogWarning("User with email '{Email}' does not exist", request.Email);
        return Error.NotFound($"User with email '{request.Email}' does not exist");
    }
}