using Api.Database;
using Api.Features.User.Create;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Api.Tests.Features.UserTests;

public class CreateUserHandlerTests : TestContainerBase
{
    [Fact]
    public async Task Handle_ShouldCreateUser_WhenUserDoesNotExist()
    {
        var options = new DbContextOptionsBuilder<ManagerContext>()
            .UseSqlServer(MsSqlContainer.GetConnectionString())
            .Options;

        await using var context = new ManagerContext(options);
        await context.Database.EnsureCreatedAsync();

        var loggerMock = new Mock<ILogger<CreateUserHandler>>();
        var handler = new CreateUserHandler(loggerMock.Object, context);

        var command = new CreateUserCommand("John", "Doe", "john.doe@example.com", "password123");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.NotNull(result.Value!.Name);
    }
}