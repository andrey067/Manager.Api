using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Manager.Api.Authentication;
using Manager.Api.Database;
using Manager.Api.Features.Users;
using Manager.Vsa.Tests.Health;
using Microsoft.Extensions.DependencyInjection;

namespace Manager.Vsa.Tests.Features.Auth;

public class RefreshTokenEndpointTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task PostRefresh_EmptyRefreshToken_ReturnsValidationError()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errorCode").GetString().Should().Be("Validation.Error");
    }

    [Fact]
    public async Task PostRefresh_InvalidToken_ReturnsProblemDetails()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken = "invalid-token" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errorCode").GetString().Should().Be("Auth.InvalidRefreshToken");
    }

    [Fact]
    public async Task PostRefresh_ValidToken_ReturnsRotatedTokens()
    {
        const string refreshToken = "stored-refresh-token";
        await SeedUserWithRefreshTokenAsync(refreshToken);

        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("accessToken").GetString().Should().NotBeNullOrWhiteSpace();
        body.GetProperty("refreshToken").GetString().Should().NotBeNullOrWhiteSpace();
        body.GetProperty("refreshToken").GetString().Should().NotBe(refreshToken);
        body.GetProperty("accessTokenExpires").GetDateTime().Should().BeAfter(DateTime.UtcNow.AddMinutes(-1));
        body.GetProperty("refreshTokenExpires").GetDateTime().Should().BeAfter(DateTime.UtcNow);
    }

    private async Task SeedUserWithRefreshTokenAsync(string refreshToken)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tokens = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var user = User.Create("Test User", "refresh@example.com", "hash");
        user.SetRefreshToken(tokens.HashRefreshToken(refreshToken), tokens.GetRefreshExpiry());
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }
}
