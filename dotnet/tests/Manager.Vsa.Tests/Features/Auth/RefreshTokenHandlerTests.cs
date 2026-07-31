using FluentAssertions;
using Manager.Api.Authentication;
using Manager.Api.Common;
using Manager.Api.Database;
using Manager.Api.Features.Auth;
using Manager.Api.Features.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Manager.Vsa.Tests.Features.Auth;

public class RefreshTokenHandlerTests
{
    private sealed class FixedDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow { get; init; } = new(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);
    }

    private static (RefreshToken.Handler Handler, ApplicationDbContext Db, JwtTokenService Tokens, FixedDateTimeProvider Clock) CreateSut(
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
        var handler = new RefreshToken.Handler(db, tokens, clock);

        return (handler, db, tokens, clock);
    }

    [Fact]
    public async Task Handle_InvalidRefreshToken_WhenTokenNotFound()
    {
        var (handler, _, _, _) = CreateSut();

        var result = await handler.Handle(new RefreshToken.Command("unknown-token"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidRefreshToken");
    }

    [Fact]
    public async Task Handle_InvalidRefreshToken_WhenTokenExpired()
    {
        var clock = new FixedDateTimeProvider();
        var (handler, db, tokens, _) = CreateSut(clock);
        const string refreshToken = "expired-refresh-token";
        var user = User.Create("Test User", "user@example.com", "hash");
        user.SetRefreshToken(tokens.HashRefreshToken(refreshToken), clock.UtcNow);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var result = await handler.Handle(new RefreshToken.Command(refreshToken), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidRefreshToken");
    }

    [Fact]
    public async Task Handle_Success_RotatesRefreshTokenHash()
    {
        var clock = new FixedDateTimeProvider();
        var (handler, db, tokens, _) = CreateSut(clock);
        const string refreshToken = "valid-refresh-token";
        var user = User.Create("Test User", "user@example.com", "hash");
        user.SetRefreshToken(tokens.HashRefreshToken(refreshToken), clock.UtcNow.AddDays(7));
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var originalHash = user.RefreshTokenHash;

        var result = await handler.Handle(new RefreshToken.Command(refreshToken), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Value.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.Value.RefreshToken.Should().NotBe(refreshToken);
        result.Value.AccessTokenExpires.Should().Be(clock.UtcNow.AddHours(1));
        result.Value.RefreshTokenExpires.Should().Be(clock.UtcNow.AddDays(7));

        var persisted = await db.Users.SingleAsync(u => u.Email == "user@example.com");
        persisted.RefreshTokenHash.Should().NotBe(originalHash);
        persisted.RefreshTokenHash.Should().Be(tokens.HashRefreshToken(result.Value.RefreshToken));
        persisted.RefreshTokenExpiresAt.Should().Be(clock.UtcNow.AddDays(7));
        persisted.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserTokenRefreshedDomainEvent>()
            .Which.Id.Should().Be(persisted.Id);
    }
}
