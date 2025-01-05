using Api.Common.Responses;
using Api.Database;
using Api.Database.Entities;
using Api.Features.User.Get;
using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Api.Tests.Features.UserTests;

public class GetUserHandlerTests : TestContainerBase
{
    [Fact]
    public async Task Handle_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ManagerContext>()
            .UseSqlServer(MsSqlContainer.GetConnectionString())
            .Options;

        var user = new Faker<User>()
            .CustomInstantiator(f =>
                User.Create(f.Name.FirstName(), f.Name.LastName(), f.Internet.Email(), f.Internet.Password()))
            .Generate();

        await using var context = new ManagerContext(options);
        await context.Database.EnsureCreatedAsync();
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<GetUserHandler>>();
        var handler = new GetUserHandler(loggerMock.Object, context);

        var command = new GetUserQuery(user.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        Assert.IsType<UserResponse>(result.Value);
        Assert.NotNull(result.Value!.Name);
    }
}