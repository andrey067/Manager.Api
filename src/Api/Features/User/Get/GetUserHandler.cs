using Api.Common.Responses;
using Api.Database;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.User.Get;

public record struct GetUserQuery(long Id) : IRequest<ErrorOr<UserResponse>>;

public class GetUserHandler(ILogger<GetUserHandler> logger, ManagerContext context)
    : IRequestHandler<GetUserQuery, ErrorOr<UserResponse>>
{
    public async Task<ErrorOr<UserResponse>> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await context.Users.SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (user is  null) return Error.NotFound($"User with ID '{request.Id}' not found");

        return new UserResponse(user.Name.ToString(), user.Email);
    }
}