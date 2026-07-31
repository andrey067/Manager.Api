using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Manager.Api.Authentication;
using Manager.Api.Common;
using Microsoft.Extensions.Configuration;

namespace Manager.Vsa.Tests.Authentication;

public class JwtTokenServiceTests
{
    private sealed class FixedDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow { get; init; } = new(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);
    }

    private static JwtTokenService CreateService(
        FixedDateTimeProvider? clock = null,
        Dictionary<string, string?>? settings = null)
    {
        clock ??= new FixedDateTimeProvider();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(settings ?? new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-signing-key-32-chars-minimum!!",
                ["Jwt:Issuer"] = "Manager.Api",
                ["Jwt:Audience"] = "Manager.Api",
                ["Jwt:HoursToExpire"] = "2",
                ["Jwt:RefreshDaysToExpire"] = "7"
            })
            .Build();

        return new JwtTokenService(config, clock);
    }

    [Fact]
    public void HashRefreshToken_ProducesStableSha256Hex()
    {
        var service = CreateService();
        const string raw = "refresh-token-value";

        var expected = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

        service.HashRefreshToken(raw).Should().Be(expected);
        service.HashRefreshToken(raw).Should().Be(expected);
    }

    [Fact]
    public void CreateAccessToken_UsesClockForExpiry()
    {
        var clock = new FixedDateTimeProvider();
        var service = CreateService(clock);

        var (token, expires) = service.CreateAccessToken(42, "user@example.com");

        token.Should().NotBeNullOrWhiteSpace();
        expires.Should().Be(clock.UtcNow.AddHours(2));
        service.TryValidateAccessToken(token, out var userId).Should().BeTrue();
        userId.Should().Be(42);
    }

    [Fact]
    public void GetAccessExpiry_UsesClock()
    {
        var clock = new FixedDateTimeProvider();
        var service = CreateService(clock);

        service.GetAccessExpiry().Should().Be(clock.UtcNow.AddHours(2));
    }

    [Fact]
    public void GetRefreshExpiry_UsesClock()
    {
        var clock = new FixedDateTimeProvider();
        var service = CreateService(clock);

        service.GetRefreshExpiry().Should().Be(clock.UtcNow.AddDays(7));
    }

    [Fact]
    public void CreateRefreshToken_ReturnsBase64String()
    {
        var service = CreateService();
        var token = service.CreateRefreshToken();
        token.Should().NotBeNullOrWhiteSpace();
        Convert.FromBase64String(token).Should().HaveCount(64);
    }
}
