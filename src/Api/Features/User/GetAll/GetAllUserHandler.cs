using Api.Common.Responses;
using Api.Database;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.User.GetAll;

internal record struct GetAllUserRespose(IEnumerable<UserResponse> Users);

internal record struct GetAllUserQuery : IRequest<ErrorOr<GetAllUserRespose>>;

internal class GetAllUserHandler(ManagerContext context) : IRequestHandler<GetAllUserQuery, ErrorOr<GetAllUserRespose>>
{
    public async Task<ErrorOr<GetAllUserRespose>> Handle(GetAllUserQuery request, CancellationToken cancellationToken)
    {
        var users = await context.Users.Select(x => new UserResponse(x.Name.ToString(), x.Email))
            .ToListAsync(cancellationToken);

        if (users.Count != 0)
            return new GetAllUserRespose(users);
        return Error.NotFound("No users found");
    }
}