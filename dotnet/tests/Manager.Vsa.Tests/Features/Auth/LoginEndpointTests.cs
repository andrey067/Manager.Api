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

public class LoginEndpointTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("", "password")]
    [InlineData("user@example.com", "")]
    public async Task PostLogin_EmptyLoginOrPassword_ReturnsValidationError(
        string login,
        string password)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { login, password });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errorCode").GetString().Should().Be("Validation.Error");
    }

    [Fact]
    public async Task PostLogin_InvalidCredentials_ReturnsProblemDetails()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { login = "nobody@example.com", password = "wrong" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errorCode").GetString().Should().Be("Auth.InvalidCredentials");
    }

    [Fact]
    public async Task PostLogin_ValidCredentials_ReturnsTokens()
    {
        await SeedUserAsync("user@example.com", "Secret123!");

        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { login = "user@example.com", password = "Secret123!" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("accessToken").GetString().Should().NotBeNullOrWhiteSpace();
        body.GetProperty("refreshToken").GetString().Should().NotBeNullOrWhiteSpace();
        body.GetProperty("accessTokenExpires").GetDateTime().Should().BeAfter(DateTime.UtcNow.AddMinutes(-1));
        body.GetProperty("refreshTokenExpires").GetDateTime().Should().BeAfter(DateTime.UtcNow);
    }

    private async Task SeedUserAsync(string email, string password)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        db.Users.Add(User.Create("Test User", email, hasher.Hash(password)));
        await db.SaveChangesAsync();
    }
}
