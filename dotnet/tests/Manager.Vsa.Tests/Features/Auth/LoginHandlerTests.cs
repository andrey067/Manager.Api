using FluentAssertions;
using Manager.Api.Authentication;
using Manager.Api.Common;
using Manager.Api.Database;
using Manager.Api.Features.Auth;
using Manager.Api.Features.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Manager.Vsa.Tests.Features.Auth;

public class LoginHandlerTests
{
    private sealed class FixedDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow { get; init; } = new(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);
    }

    private static (Login.Handler Handler, ApplicationDbContext Db, JwtTokenService Tokens, FixedDateTimeProvider Clock) CreateSut(
        FixedDateTimeProvider? clock = null)
    {
        clock ??= new FixedDateTimeProvider();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new ApplicationDbContext(options);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-signing-key-32-chars-minimum!!",
                ["Jwt:Issuer"] = "Manager.Api",
                ["Jwt:Audience"] = "Manager.Api",
                ["Jwt:HoursToExpire"] = "1",
                ["Jwt:RefreshDaysToExpire"] = "7"
            })
            .Build();

        var tokens = new JwtTokenService(config, clock);
        var hasher = new TestPasswordHasher();
        var handler = new Login.Handler(db, hasher, tokens);

        return (handler, db, tokens, clock);
    }

    [Fact]
    public async Task Handle_InvalidCredentials_WhenUserNotFound()
    {
        var (handler, _, _, _) = CreateSut();

        var result = await handler.Handle(new Login.Command("missing@example.com", "secret"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
    }

    [Fact]
    public async Task Handle_InvalidCredentials_WhenPasswordWrong()
    {
        var (handler, db, _, _) = CreateSut();
        var hasher = new TestPasswordHasher();
        db.Users.Add(User.Create("Test User", "user@example.com", hasher.Hash("correct")));
        await db.SaveChangesAsync();

        var result = await handler.Handle(new Login.Command("user@example.com", "wrong"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
    }

    [Fact]
    public async Task Handle_TrimsLogin()
    {
        var (handler, db, _, _) = CreateSut();
        var hasher = new TestPasswordHasher();
        const string password = "Secret123!";
        db.Users.Add(User.Create("Test User", "user@example.com", hasher.Hash(password)));
        await db.SaveChangesAsync();

        var result = await handler.Handle(
            new Login.Command("  user@example.com  ", password),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Success_PersistsRefreshTokenHash()
    {
        var clock = new FixedDateTimeProvider();
        var (handler, db, tokens, _) = CreateSut(clock);
        var hasher = new TestPasswordHasher();
        const string password = "Secret123!";
        var user = User.Create("Test User", "user@example.com", hasher.Hash(password));
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var result = await handler.Handle(new Login.Command("user@example.com", password), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Value.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.Value.AccessTokenExpires.Should().Be(clock.UtcNow.AddHours(1));
        result.Value.RefreshTokenExpires.Should().Be(clock.UtcNow.AddDays(7));

        var persisted = await db.Users.SingleAsync(u => u.Email == "user@example.com");
        persisted.RefreshTokenHash.Should().Be(tokens.HashRefreshToken(result.Value.RefreshToken));
        persisted.RefreshTokenExpiresAt.Should().Be(clock.UtcNow.AddDays(7));
        persisted.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserLoggedInDomainEvent>()
            .Which.Id.Should().Be(persisted.Id);
    }

    private sealed class TestPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hash:{password}";

        public bool Verify(string password, string hash) => hash == Hash(password);
    }
}
