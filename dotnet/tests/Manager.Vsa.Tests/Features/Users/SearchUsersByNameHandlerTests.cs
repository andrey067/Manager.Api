using FluentAssertions;
using Manager.Api.Authentication;
using Manager.Api.Database;
using Manager.Api.Features.Users;
using Microsoft.EntityFrameworkCore;

namespace Manager.Vsa.Tests.Features.Users;

public class SearchUsersByNameHandlerTests
{
    private static async Task<(SearchUsersByName.Handler Handler, ApplicationDbContext Db)> CreateSut()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new ApplicationDbContext(options);
        var handler = new SearchUsersByName.Handler(db);

        return (handler, db);
    }

    [Fact]
    public async Task Handle_ReturnsMatchingUsers_CaseInsensitive()
    {
        var (handler, db) = await CreateSut();
        var hasher = new TestPasswordHasher();
        db.Users.Add(User.Create("Alice Smith", "alice@example.com", hasher.Hash("Password1!")));
        db.Users.Add(User.Create("Bob Alice", "bob@example.com", hasher.Hash("Password1!")));
        db.Users.Add(User.Create("Charlie", "charlie@example.com", hasher.Hash("Password1!")));
        await db.SaveChangesAsync();

        var result = await handler.Handle(new SearchUsersByName.Query("alice"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Select(u => u.Name).Should().Contain("Alice Smith").And.Contain("Bob Alice");
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenNoMatch()
    {
        var (handler, db) = await CreateSut();
        var hasher = new TestPasswordHasher();
        db.Users.Add(User.Create("Alice", "alice@example.com", hasher.Hash("Password1!")));
        await db.SaveChangesAsync();

        var result = await handler.Handle(new SearchUsersByName.Query("nobody"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    private sealed class TestPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hash:{password}";

        public bool Verify(string password, string hash) => hash == Hash(password);
    }
}
