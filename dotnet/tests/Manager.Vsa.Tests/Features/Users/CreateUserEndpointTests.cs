using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Manager.Api.Authentication;
using Manager.Api.Database;
using Manager.Api.Features.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Manager.Vsa.Tests.Features.Users;

[Collection("CreateUser")]
public class CreateUserEndpointTests(CreateUserWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task PostUsers_WithoutJwt_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/users",
            new { name = "New User", email = "new@example.com", password = "Password1!" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostUsers_WithJwt_CreatesUser()
    {
        await SeedUserAsync("admin@example.com", "Admin User", "Password1!");
        var token = await LoginAsync("admin@example.com", "Password1!");

        var response = await PostUsersAsync(
            token,
            new { name = "Created User", email = "created@example.com", password = "Password1!" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetInt64().Should().BeGreaterThan(0);
        body.GetProperty("name").GetString().Should().Be("Created User");
        body.GetProperty("email").GetString().Should().Be("created@example.com");
    }

    [Fact]
    public async Task PostUsers_DuplicateEmail_ReturnsEmailConflict()
    {
        await SeedUserAsync("admin@example.com", "Admin User", "Password1!");
        await SeedUserAsync("existing@example.com", "Existing User");
        var token = await LoginAsync("admin@example.com", "Password1!");

        var response = await PostUsersAsync(
            token,
            new { name = "Duplicate", email = "existing@example.com", password = "Password1!" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errorCode").GetString().Should().Be("Users.EmailConflict");
    }

    private async Task<HttpResponseMessage> PostUsersAsync(string token, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users")
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request);
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { login = email, password });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("accessToken").GetString()!;
    }

    private async Task SeedUserAsync(
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
    }
}
