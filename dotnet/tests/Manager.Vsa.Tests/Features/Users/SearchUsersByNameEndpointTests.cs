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
public class SearchUsersByNameEndpointTests(CreateUserWebApplicationFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync() => await factory.ResetStateAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SearchUsersByName_WithoutJwt_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/users/search-by-name?name=alice");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SearchUsersByName_WithJwt_ReturnsMatchingUsers()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var adminId = await SeedUserAsync($"admin-{suffix}@example.com", $"Alice Smith {suffix}", "Password1!");
        await SeedUserAsync($"bob-{suffix}@example.com", $"Bob Alice {suffix}");
        await SeedUserAsync($"charlie-{suffix}@example.com", "Charlie");
        var token = factory.IssueToken(adminId, $"admin-{suffix}@example.com");

        var response = await SearchUsersByNameAsync(token, suffix);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.ValueKind.Should().Be(JsonValueKind.Array);
        body.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task SearchUsersByName_WithJwt_ReturnsEmptyList_WhenNoMatch()
    {
        var adminId = await SeedUserAsync("admin@example.com", "Alice", "Password1!");
        var token = factory.IssueToken(adminId, "admin@example.com");

        var response = await SearchUsersByNameAsync(token, "nobody");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.ValueKind.Should().Be(JsonValueKind.Array);
        body.GetArrayLength().Should().Be(0);
    }

    private async Task<HttpResponseMessage> SearchUsersByNameAsync(string token, string name)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/users/search-by-name?name={Uri.EscapeDataString(name)}");
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
