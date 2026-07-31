using FluentAssertions;
using Manager.Api.Authentication;
using Manager.Api.Database;
using Manager.Api.Features.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace Manager.Vsa.Tests.Features.Users;

public class GetUserByEmailHandlerTests
{
    private static async Task<(GetUserByEmail.Handler Handler, ApplicationDbContext Db, HybridCache Cache)> CreateSut()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new ApplicationDbContext(options);
        var services = new ServiceCollection();
        services.AddHybridCache();
        var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<HybridCache>();
        var handler = new GetUserByEmail.Handler(db, cache);

        return (handler, db, cache);
    }

    [Fact]
    public async Task Handle_NotFound_WhenUserDoesNotExist()
    {
        var (handler, _, _) = await CreateSut();

        var result = await handler.Handle(new GetUserByEmail.Query("missing@example.com"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Users.NotFound");
    }

    [Fact]
    public async Task Handle_Success_LoadsFromDatabaseAndCaches()
    {
        var (handler, db, cache) = await CreateSut();
        var hasher = new TestPasswordHasher();
        var user = User.Create("Cached User", "cached@example.com", hasher.Hash("Password1!"));
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var result = await handler.Handle(new GetUserByEmail.Query("cached@example.com"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(user.Id);
        result.Value.Name.Should().Be("Cached User");
        result.Value.Email.Should().Be("cached@example.com");

        var cached = await cache.GetOrCreateAsync(
            UserCacheKeys.ByEmail("cached@example.com"),
            _ => ValueTask.FromResult(new GetUserByEmail.Response(-1, "stale", "stale@example.com")));
        cached.Id.Should().Be(user.Id);
        cached.Name.Should().Be("Cached User");
    }

    [Fact]
    public async Task Handle_NotFound_DoesNotPoisonCache_AllowsLaterSuccess()
    {
        var (handler, db, _) = await CreateSut();

        var miss = await handler.Handle(
            new GetUserByEmail.Query("later@example.com"),
            CancellationToken.None);
        miss.IsFailure.Should().BeTrue();

        var hasher = new TestPasswordHasher();
        var user = User.Create("Later", "later@example.com", hasher.Hash("Password1!"));
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var hit = await handler.Handle(
            new GetUserByEmail.Query("later@example.com"),
            CancellationToken.None);
        hit.IsSuccess.Should().BeTrue();
        hit.Value.Email.Should().Be("later@example.com");
    }

    [Fact]
    public async Task Handle_CacheHit_ReturnsCachedWithoutDatabase()
    {
        var (handler, db, cache) = await CreateSut();
        var cachedResponse = new GetUserByEmail.Response(42, "From Cache", "cache@example.com");
        await cache.SetAsync(UserCacheKeys.ByEmail("cache@example.com"), cachedResponse);

        var result = await handler.Handle(new GetUserByEmail.Query("cache@example.com"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(cachedResponse);
        (await db.Users.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Handle_EmailMatch_IsCaseInsensitive()
    {
        var (handler, db, _) = await CreateSut();
        var hasher = new TestPasswordHasher();
        var user = User.Create("Case User", "User@Example.COM", hasher.Hash("Password1!"));
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var result = await handler.Handle(new GetUserByEmail.Query("user@example.com"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(user.Id);
    }

    private sealed class TestPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hash:{password}";

        public bool Verify(string password, string hash) => hash == Hash(password);
    }
}
