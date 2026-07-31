using FluentAssertions;
using Manager.Api.Authentication;
using Manager.Api.Database;
using Manager.Api.Features.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace Manager.Vsa.Tests.Features.Users;

public class GetAllUsersHandlerTests
{
    private static async Task<(GetAllUsers.Handler Handler, ApplicationDbContext Db, HybridCache Cache)> CreateSut()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new ApplicationDbContext(options);
        var services = new ServiceCollection();
        services.AddHybridCache();
        var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<HybridCache>();
        var handler = new GetAllUsers.Handler(db, cache);

        return (handler, db, cache);
    }

    [Fact]
    public async Task Handle_Success_LoadsFromDatabaseAndCaches()
    {
        var (handler, db, cache) = await CreateSut();
        var hasher = new TestPasswordHasher();
        db.Users.Add(User.Create("Alice", "alice@example.com", hasher.Hash("Password1!")));
        db.Users.Add(User.Create("Bob", "bob@example.com", hasher.Hash("Password1!")));
        await db.SaveChangesAsync();

        var result = await handler.Handle(new GetAllUsers.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Select(u => u.Email).Should().BeEquivalentTo(["alice@example.com", "bob@example.com"]);

        var cached = await cache.GetOrCreateAsync(
            UserCacheKeys.All,
            _ => ValueTask.FromResult(new List<GetAllUsers.Response>()));
        cached.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_CacheHit_ReturnsCachedWithoutDatabase()
    {
        var (handler, db, cache) = await CreateSut();
        var cachedList = new List<GetAllUsers.Response>
        {
            new(1, "From Cache", "cache@example.com")
        };
        await cache.SetAsync(UserCacheKeys.All, cachedList);

        var result = await handler.Handle(new GetAllUsers.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(cachedList);
        (await db.Users.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Handle_AfterCreateUserInvalidation_RefreshesList()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new ApplicationDbContext(options);
        var services = new ServiceCollection();
        services.AddHybridCache();
        var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<HybridCache>();
        var hasher = new TestPasswordHasher();

        var getAllHandler = new GetAllUsers.Handler(db, cache);
        var createHandler = new CreateUser.Handler(db, hasher, cache);

        db.Users.Add(User.Create("Existing", "existing@example.com", hasher.Hash("Password1!")));
        await db.SaveChangesAsync();

        var first = await getAllHandler.Handle(new GetAllUsers.Query(), CancellationToken.None);
        first.IsSuccess.Should().BeTrue();
        first.Value.Should().HaveCount(1);

        await createHandler.Handle(
            new CreateUser.Command("New User", "new@example.com", "Password1!"),
            CancellationToken.None);

        var second = await getAllHandler.Handle(new GetAllUsers.Query(), CancellationToken.None);
        second.IsSuccess.Should().BeTrue();
        second.Value.Should().HaveCount(2);
        second.Value.Select(u => u.Email).Should().Contain("new@example.com");
    }

    private sealed class TestPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hash:{password}";

        public bool Verify(string password, string hash) => hash == Hash(password);
    }
}
