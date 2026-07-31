using FluentAssertions;
using Manager.Api.Database;
using Manager.Api.Features.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace Manager.Vsa.Tests.Features.Users;

public class RemoveUserHandlerTests
{
    private static async Task<(RemoveUser.Handler Handler, ApplicationDbContext Db, HybridCache Cache)> CreateSut()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new ApplicationDbContext(options);
        var services = new ServiceCollection();
        services.AddHybridCache();
        var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<HybridCache>();
        var handler = new RemoveUser.Handler(db, cache);

        return (handler, db, cache);
    }

    [Fact]
    public async Task Handle_NotFound_WhenUserDoesNotExist()
    {
        var (handler, db, _) = await CreateSut();

        var result = await handler.Handle(new RemoveUser.Command(999), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Users.NotFound");
        (await db.Users.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Handle_Success_RemovesUserRaisesEventAndInvalidatesCache()
    {
        var (handler, db, cache) = await CreateSut();
        const string email = "remove@example.com";
        db.Users.Add(User.Create("Remove Me", email, "hash:Password1!"));
        await db.SaveChangesAsync();
        var target = await db.Users.SingleAsync();

        await cache.SetAsync(UserCacheKeys.All, "cached-all");
        await cache.SetAsync(UserCacheKeys.ById(target.Id), "cached-by-id");
        await cache.SetAsync(UserCacheKeys.ByEmail(email), "cached-by-email");

        var result = await handler.Handle(new RemoveUser.Command(target.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await db.Users.CountAsync()).Should().Be(0);

        (await cache.GetOrCreateAsync(UserCacheKeys.All, _ => ValueTask.FromResult("refreshed-all")))
            .Should().Be("refreshed-all");
        (await cache.GetOrCreateAsync(UserCacheKeys.ById(target.Id), _ => ValueTask.FromResult("refreshed-id")))
            .Should().Be("refreshed-id");
        (await cache.GetOrCreateAsync(UserCacheKeys.ByEmail(email), _ => ValueTask.FromResult("refreshed-email")))
            .Should().Be("refreshed-email");
    }
}
