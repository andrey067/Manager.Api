using Api.Database;
using Api.Features.User.GetAll;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.User.SearchByEmail;

internal record struct SearchByEmailQuery(string Email) : IRequest<ErrorOr<List<UserResponse>>>;

internal class SearchByEmailHandler(ILogger<SearchByEmailHandler> logger, ManagerContext context)
    : IRequestHandler<SearchByEmailQuery, ErrorOr<List<UserResponse>>>
{
    public async Task<ErrorOr<List<UserResponse>>> Handle(SearchByEmailQuery request,
        CancellationToken cancellationToken)
    {
        var users = await context.Users
            .Where(u => u.Email == request.Email)
            .Select(u => new UserResponse(u.Name.ToString(), u.Email))
            .ToListAsync(cancellationToken);

        if (users.Any())
            return users;

        logger.LogWarning("No users found with email '{Email}'", request.Email);
        return Error.NotFound($"No users found with email '{request.Email}'");
    }
}