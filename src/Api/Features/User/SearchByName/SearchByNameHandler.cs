using Api.Database;
using Api.Features.User.GetAll;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.User.SearchByName;

internal record struct SearchByNameQuery(string Name) : IRequest<ErrorOr<List<UserResponse>>>;

internal class SearchByNameHandler(ILogger<SearchByNameHandler> logger, ManagerContext context)
    : IRequestHandler<SearchByNameQuery, ErrorOr<List<UserResponse>>>
{
    public async Task<ErrorOr<List<UserResponse>>> Handle(SearchByNameQuery request,
        CancellationToken cancellationToken)
    {
        var users = await context.Users
            .Where(u => u.Name.Value.FirstName.Contains(request.Name) || u.Name.Value.FirstName.Contains(request.Name))
            .Include(user => user.Id)
            .ToListAsync(cancellationToken);

        var response = users.Select(u => new UserResponse(u.Name.ToString(), u.Email)).ToList();
        return response;
    }
}