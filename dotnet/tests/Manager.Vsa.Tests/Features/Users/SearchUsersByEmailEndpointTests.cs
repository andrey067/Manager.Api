using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Manager.Api.Authentication;
using Manager.Api.Database;
using Manager.Api.Features.Users;
using Microsoft.Extensions.DependencyInjection;

namespace Manager.Vsa.Tests.Features.Users;

[Collection("CreateUser")]
public class SearchUsersByEmailEndpointTests(CreateUserWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task SearchUsersByEmail_WithoutJwt_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/users/search-by-email?email=example.com");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SearchUsersByEmail_WithJwt_ReturnsMatchingUsers()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var domain = $"{suffix}.example.com";
        var adminId = await SeedUserAsync($"one@{domain}", "User One", "Password1!");
        await SeedUserAsync($"two@{domain.ToUpperInvariant()}", "User Two");
        await SeedUserAsync("other@test.com", "Other");
        var token = factory.IssueToken(adminId, $"one@{domain}");

        var response = await SearchUsersByEmailAsync(token, domain);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.ValueKind.Should().Be(JsonValueKind.Array);
        body.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task SearchUsersByEmail_WithJwt_ReturnsEmptyList_WhenNoMatch()
    {
        var adminId = await SeedUserAsync("admin@example.com", "Admin", "Password1!");
        var token = factory.IssueToken(adminId, "admin@example.com");

        var response = await SearchUsersByEmailAsync(token, "none@example.com");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.ValueKind.Should().Be(JsonValueKind.Array);
        body.GetArrayLength().Should().Be(0);
    }

    private async Task<HttpResponseMessage> SearchUsersByEmailAsync(string token, string email)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/users/search-by-email?email={Uri.EscapeDataString(email)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request);
    }

    private async Task<long> SeedUserAsync(
        string email,
        string name = "Admin User",
        string password = "Password1!")
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var user = User.Create(name, email, hasher.Hash(password));
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }
}
