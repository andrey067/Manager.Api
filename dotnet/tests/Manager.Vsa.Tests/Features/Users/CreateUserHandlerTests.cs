using FluentAssertions;
using Manager.Api.Authentication;
using Manager.Api.Database;
using Manager.Api.Features.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace Manager.Vsa.Tests.Features.Users;

public class CreateUserHandlerTests
{
    private static async Task<(CreateUser.Handler Handler, ApplicationDbContext Db, HybridCache Cache)> CreateSut()
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
        var handler = new CreateUser.Handler(db, hasher, cache);

        return (handler, db, cache);
    }

    [Fact]
    public async Task Handle_EmailConflict_WhenEmailExists()
    {
        var (handler, db, _) = await CreateSut();
        var hasher = new TestPasswordHasher();
        db.Users.Add(User.Create("Existing", "existing@example.com", hasher.Hash("password")));
        await db.SaveChangesAsync();

        var result = await handler.Handle(
            new CreateUser.Command("New User", "existing@example.com", "Password1!"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Users.EmailConflict");
        (await db.Users.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Handle_TrimsNameAndEmail()
    {
        var (handler, db, _) = await CreateSut();

        var result = await handler.Handle(
            new CreateUser.Command("  Ada  ", "  ada@example.com  ", "Password1!"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await db.Users.SingleAsync()).Email.Should().Be("ada@example.com");
        (await db.Users.SingleAsync()).Name.Should().Be("Ada");
    }

    [Fact]
    public async Task Handle_Success_CreatesUserRaisesEventAndInvalidatesCache()
    {
        var (handler, db, cache) = await CreateSut();
        const string password = "Password1!";
        const string email = "new@example.com";
        await cache.SetAsync(UserCacheKeys.All, "cached");
        await cache.SetAsync(UserCacheKeys.ByEmail(email), "cached-by-email");

        var result = await handler.Handle(
            new CreateUser.Command("New User", email, password),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().BeGreaterThan(0);
        result.Value.Name.Should().Be("New User");
        result.Value.Email.Should().Be(email);

        var persisted = await db.Users.SingleAsync();
        persisted.Name.Should().Be("New User");
        persisted.Email.Should().Be(email);
        persisted.Password.Should().Be($"hash:{password}");

        persisted.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserCreatedDomainEvent>()
            .Which.Id.Should().Be(persisted.Id);

        var cachedAll = await cache.GetOrCreateAsync(
            UserCacheKeys.All,
            _ => ValueTask.FromResult("refreshed-all"));
        cachedAll.Should().Be("refreshed-all");

        var cachedEmail = await cache.GetOrCreateAsync(
            UserCacheKeys.ByEmail(email),
            _ => ValueTask.FromResult("refreshed-email"));
        cachedEmail.Should().Be("refreshed-email");
    }

    private sealed class TestPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hash:{password}";

        public bool Verify(string password, string hash) => hash == Hash(password);
    }
}
