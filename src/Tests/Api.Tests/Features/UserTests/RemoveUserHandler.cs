using Api.Database;
using Api.Database.Entities;
using Api.Features.User.Remove;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Api.Tests.Features.UserTests;

public class RemoveUserHandlerTests : TestContainerBase
{
    [Fact]
    public async Task Handle_RemovesUser_WhenUserExists()
    {
        var options = new DbContextOptionsBuilder<ManagerContext>()
            .UseSqlServer(MsSqlContainer.GetConnectionString())
            .Options;

        await using var context = new ManagerContext(options);
        await context.Database.EnsureCreatedAsync();

        var loggerMock = new Mock<ILogger<RemoveUserHandler>>();
        var user = User.Create("John", "Doe", "john.doe@example.com", "password123");
        user.GetType().GetProperty("Id")!.SetValue(user, 1);
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        var handler = new RemoveUserHandler(loggerMock.Object, context);
        var query = new RemoveUserQuery(1);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Null(await context.Users.SingleOrDefaultAsync(x => x.Id.Value == 1));
    }

    [Fact]
    public async Task Handle_ReturnsNotFoundError_WhenUserDoesNotExist()
    {
        var options = new DbContextOptionsBuilder<ManagerContext>()
            .UseSqlServer(MsSqlContainer.GetConnectionString())
            .Options;

        await using var context = new ManagerContext(options);
        await context.Database.EnsureCreatedAsync();

        var loggerMock = new Mock<ILogger<RemoveUserHandler>>();
        var handler = new RemoveUserHandler(loggerMock.Object, context);
        var query = new RemoveUserQuery(1);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal("User with ID '1' not found", result.Errors.First().Description);
    }
}