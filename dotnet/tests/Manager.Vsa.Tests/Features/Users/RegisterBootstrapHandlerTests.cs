using FluentAssertions;
using Manager.Api.Authentication;
using Manager.Api.Database;
using Manager.Api.Features.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace Manager.Vsa.Tests.Features.Users;

public class RegisterBootstrapHandlerTests
{
    private static async Task<(RegisterBootstrap.Handler Handler, ApplicationDbContext Db, HybridCache Cache)> CreateSut()
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
        var handler = new RegisterBootstrap.Handler(db, hasher, cache);

        return (handler, db, cache);
    }

    [Fact]
    public async Task Handle_BootstrapNotAllowed_WhenUsersExist()
    {
        var (handler, db, _) = await CreateSut();
        var hasher = new TestPasswordHasher();
        db.Users.Add(User.Create("Existing", "existing@example.com", hasher.Hash("password")));
        await db.SaveChangesAsync();

        var result = await handler.Handle(
            new RegisterBootstrap.Command("New User", "new@example.com", "Password1!"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Users.BootstrapNotAllowed");
        (await db.Users.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Handle_Success_CreatesUserRaisesEventAndInvalidatesCache()
    {
        var (handler, db, cache) = await CreateSut();
        const string password = "Password1!";
        await cache.SetAsync(UserCacheKeys.All, "cached");

        var result = await handler.Handle(
            new RegisterBootstrap.Command("Admin User", "admin@example.com", password),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().BeGreaterThan(0);
        result.Value.Name.Should().Be("Admin User");
        result.Value.Email.Should().Be("admin@example.com");

        var persisted = await db.Users.SingleAsync();
        persisted.Name.Should().Be("Admin User");
        persisted.Email.Should().Be("admin@example.com");
        persisted.Password.Should().Be($"hash:{password}");

        persisted.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserCreatedDomainEvent>()
            .Which.Id.Should().Be(persisted.Id);

        var cached = await cache.GetOrCreateAsync(
            UserCacheKeys.All,
            _ => ValueTask.FromResult("refreshed"));
        cached.Should().Be("refreshed");
    }

    private sealed class TestPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hash:{password}";

        public bool Verify(string password, string hash) => hash == Hash(password);
    }
}
