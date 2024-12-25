using System.Linq.Expressions;
using Api.Database;
using Api.Database.Entities;
using Api.Features.User.Update;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Api.Tests.Features.UserTests;

public class UpdateUserHandlerTests : TestContainerBase
{
    [Fact]
    public async Task Handle_ReturnsUpdatedUserResponse_WhenUserExists()
    {
        var options = new DbContextOptionsBuilder<ManagerContext>()
            .UseSqlServer(MsSqlContainer.GetConnectionString())
            .Options;

        await using var context = new ManagerContext(options);
        await context.Database.EnsureCreatedAsync();

        var loggerMock = new Mock<ILogger<UpdateUserHandler>>();
        var user = User.Create("John", "Doe", "john.doe@example.com", "password123");
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        var handler = new UpdateUserHandler(loggerMock.Object, context);
        var command = new UpdateUserCommand(1, "Jane", "Doe", "jane.doe@example.com", "newpassword123");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal(1, result.Value.Id);
        Assert.Equal("Jane Doe", result.Value.Name);
        Assert.Equal("jane.doe@example.com", result.Value.Email);
    }

    [Fact]
    public async Task Handle_ReturnsNotFoundError_WhenUserDoesNotExist()
    {
        var options = new DbContextOptionsBuilder<ManagerContext>()
            .UseSqlServer(MsSqlContainer.GetConnectionString())
            .Options;

        await using var context = new ManagerContext(options);
        await context.Database.EnsureCreatedAsync();

        var loggerMock = new Mock<ILogger<UpdateUserHandler>>();
        var handler = new UpdateUserHandler(loggerMock.Object, context);
        var command = new UpdateUserCommand(1, "Jane", "Doe", "jane.doe@example.com", "newpassword123");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal("User with email 'jane.doe@example.com' not found", result.Errors.First().Description);
    }
}