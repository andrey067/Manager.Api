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
public class GetUserEndpointTests(CreateUserWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetUser_WithoutJwt_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/users/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUser_WithJwt_ReturnsUser()
    {
        var userId = await SeedUserAsync("admin@example.com", "Admin User", "Password1!");
        var token = factory.IssueToken(userId, "admin@example.com");

        var response = await GetUserAsync(token, userId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetInt64().Should().Be(userId);
        body.GetProperty("name").GetString().Should().Be("Admin User");
        body.GetProperty("email").GetString().Should().Be("admin@example.com");
    }

    [Fact]
    public async Task GetUser_NotFound_ReturnsUsersNotFound()
    {
        var userId = await SeedUserAsync("admin@example.com", "Admin User", "Password1!");
        var token = factory.IssueToken(userId, "admin@example.com");

        var response = await GetUserAsync(token, 9999);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errorCode").GetString().Should().Be("Users.NotFound");
    }

    private async Task<HttpResponseMessage> GetUserAsync(string token, long id)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/users/{id}");
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
