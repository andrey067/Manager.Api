using FluentAssertions;
using Manager.Api.Authentication;
using Manager.Api.Database;
using Manager.Api.Features.Users;
using Microsoft.EntityFrameworkCore;

namespace Manager.Vsa.Tests.Features.Users;

public class SearchUsersByEmailHandlerTests
{
    private static async Task<(SearchUsersByEmail.Handler Handler, ApplicationDbContext Db)> CreateSut()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new ApplicationDbContext(options);
        var handler = new SearchUsersByEmail.Handler(db);

        return (handler, db);
    }

    [Fact]
    public async Task Handle_ReturnsMatchingUsers_CaseInsensitive()
    {
        var (handler, db) = await CreateSut();
        var hasher = new TestPasswordHasher();
        db.Users.Add(User.Create("User One", "one@example.com", hasher.Hash("Password1!")));
        db.Users.Add(User.Create("User Two", "two@EXAMPLE.com", hasher.Hash("Password1!")));
        db.Users.Add(User.Create("Other", "other@test.com", hasher.Hash("Password1!")));
        await db.SaveChangesAsync();

        var result = await handler.Handle(new SearchUsersByEmail.Query("example.com"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Select(u => u.Email).Should().Contain("one@example.com").And.Contain("two@EXAMPLE.com");
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenNoMatch()
    {
        var (handler, db) = await CreateSut();
        var hasher = new TestPasswordHasher();
        db.Users.Add(User.Create("Alice", "alice@example.com", hasher.Hash("Password1!")));
        await db.SaveChangesAsync();

        var result = await handler.Handle(new SearchUsersByEmail.Query("none@example.com"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    private sealed class TestPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hash:{password}";

        public bool Verify(string password, string hash) => hash == Hash(password);
    }
}
