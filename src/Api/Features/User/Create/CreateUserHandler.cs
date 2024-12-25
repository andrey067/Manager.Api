using Api.Database;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UserEntity = Api.Database.Entities.User;

namespace Api.Features.User.Create;

public record struct CreateUserCommand(string FirstName, string LastName, string Email, string Password)
    : IRequest<ErrorOr<CreateUserResponse>>;

public class CreateUserHandler(ILogger<CreateUserHandler> logger, ManagerContext context)
    : IRequestHandler<CreateUserCommand, ErrorOr<CreateUserResponse>>
{
    public async Task<ErrorOr<CreateUserResponse>> Handle(CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user != null)
        {
            logger.LogInformation("User with email '{Email}' already exists", request.Email);
            return Error.Conflict($"User with email '{request.Email}' already exists");
        }

        //TODO - Implementar validacao de entidade
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var userToCreate = UserEntity.Create(request.FirstName, request.LastName, request.Email, passwordHash);

        await context.Users.AddAsync(userToCreate, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Created user: {@User}", userToCreate);
        return new CreateUserResponse(userToCreate.Id.Value, user!.Name.ToString(), userToCreate.Email);
    }
}