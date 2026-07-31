using FluentAssertions;
using Manager.Api.Authentication;
using Manager.Api.Database;
using Manager.Api.Features.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace Manager.Vsa.Tests.Features.Users;

public class UpdateUserHandlerTests
{
    private static async Task<(UpdateUser.Handler Handler, ApplicationDbContext Db, HybridCache Cache)> CreateSut()
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
        var handler = new UpdateUser.Handler(db, hasher, cache);

        return (handler, db, cache);
    }

    [Fact]
    public async Task Handle_NotFound_WhenUserDoesNotExist()
    {
        var (handler, db, _) = await CreateSut();

        var result = await handler.Handle(
            new UpdateUser.Command(999, "Updated", "updated@example.com", "Password1!"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Users.NotFound");
        (await db.Users.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Handle_EmailConflict_WhenEmailBelongsToAnotherUser()
    {
        var (handler, db, _) = await CreateSut();
        var hasher = new TestPasswordHasher();
        db.Users.Add(User.Create("User One", "one@example.com", hasher.Hash("Password1!")));
        db.Users.Add(User.Create("User Two", "two@example.com", hasher.Hash("Password1!")));
        await db.SaveChangesAsync();
        var target = await db.Users.SingleAsync(u => u.Email == "one@example.com");

        var result = await handler.Handle(
            new UpdateUser.Command(target.Id, "User One", "two@example.com", "Password1!"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Users.EmailConflict");
        var unchanged = await db.Users.SingleAsync(u => u.Id == target.Id);
        unchanged.Email.Should().Be("one@example.com");
    }

    [Fact]
    public async Task Handle_TrimsNameAndEmail()
    {
        var (handler, db, _) = await CreateSut();
        var hasher = new TestPasswordHasher();
        db.Users.Add(User.Create("Old Name", "old@example.com", hasher.Hash("Password1!")));
        await db.SaveChangesAsync();
        var target = await db.Users.SingleAsync();

        var result = await handler.Handle(
            new UpdateUser.Command(target.Id, "  New Name  ", "  new@example.com  ", "Password1!"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await db.Users.SingleAsync()).Email.Should().Be("new@example.com");
        (await db.Users.SingleAsync()).Name.Should().Be("New Name");
    }

    [Fact]
    public async Task Handle_Success_UpdatesUserRaisesEventAndInvalidatesCache()
    {
        var (handler, db, cache) = await CreateSut();
        var hasher = new TestPasswordHasher();
        const string oldEmail = "old@example.com";
        const string newEmail = "new@example.com";
        const string password = "Password1!";
        db.Users.Add(User.Create("Old Name", oldEmail, hasher.Hash(password)));
        await db.SaveChangesAsync();
        var target = await db.Users.SingleAsync();

        await cache.SetAsync(UserCacheKeys.All, "cached-all");
        await cache.SetAsync(UserCacheKeys.ById(target.Id), "cached-by-id");
        await cache.SetAsync(UserCacheKeys.ByEmail(oldEmail), "cached-old-email");
        await cache.SetAsync(UserCacheKeys.ByEmail(newEmail), "cached-new-email");

        var result = await handler.Handle(
            new UpdateUser.Command(target.Id, "New Name", newEmail, password),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(target.Id);
        result.Value.Name.Should().Be("New Name");
        result.Value.Email.Should().Be(newEmail);

        var persisted = await db.Users.SingleAsync();
        persisted.Name.Should().Be("New Name");
        persisted.Email.Should().Be(newEmail);
        persisted.Password.Should().Be(hasher.Hash(password));

        persisted.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserUpdatedDomainEvent>()
            .Which.Id.Should().Be(target.Id);

        (await cache.GetOrCreateAsync(UserCacheKeys.All, _ => ValueTask.FromResult("refreshed-all")))
            .Should().Be("refreshed-all");
        (await cache.GetOrCreateAsync(UserCacheKeys.ById(target.Id), _ => ValueTask.FromResult("refreshed-id")))
            .Should().Be("refreshed-id");
        (await cache.GetOrCreateAsync(UserCacheKeys.ByEmail(oldEmail), _ => ValueTask.FromResult("refreshed-old")))
            .Should().Be("refreshed-old");
        (await cache.GetOrCreateAsync(UserCacheKeys.ByEmail(newEmail), _ => ValueTask.FromResult("refreshed-new")))
            .Should().Be("refreshed-new");
    }

    private sealed class TestPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hash:{password}";

        public bool Verify(string password, string hash) => hash == Hash(password);
    }
}
