using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Manager.Api.Authentication;
using Manager.Api.Database;
using Manager.Api.Features.Users;
using Manager.Vsa.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Manager.Vsa.Tests.Features.Users;

[Collection(PostgresCollection.Name)]
public class UpdateUserEndpointTests : IAsyncLifetime
{
    private readonly CreateUserWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UpdateUserEndpointTests(PostgresFixture postgres)
    {
        _factory = new CreateUserWebApplicationFactory(postgres);
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync() => await _factory.ResetStateAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task PutUsers_WithoutJwt_ReturnsUnauthorized()
    {
        var response = await _client.PutAsJsonAsync(
            "/api/v1/users/1",
            new { name = "Updated", email = "updated@example.com", password = "Password1!" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }


    [Fact]
    public async Task PutUsers_InvalidBody_ReturnsValidationError()
    {
        var target = await SeedUserAsync("target@example.com", "Target User", "Password1!");
        await SeedUserAsync("admin@example.com", "Admin User", "Password1!");
        var token = await LoginAsync("admin@example.com", "Password1!");

        var response = await PutUsersAsync(
            token,
            target.Id,
            new { name = "Updated User", email = "updated@example.com", password = "short" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errorCode").GetString().Should().Be("Validation.Error");
    }

    [Fact]
    public async Task PutUsers_EmptyName_ReturnsValidationError()
    {
        var target = await SeedUserAsync("target@example.com", "Target User", "Password1!");
        await SeedUserAsync("admin@example.com", "Admin User", "Password1!");
        var token = await LoginAsync("admin@example.com", "Password1!");

        var response = await PutUsersAsync(
            token,
            target.Id,
            new { name = "", email = "a@b.com", password = "Password1!" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errorCode").GetString().Should().Be("Validation.Error");
    }

    [Fact]
    public async Task PutUsers_WithJwt_UpdatesUser()
    {
        var target = await SeedUserAsync("target@example.com", "Target User", "Password1!");
        await SeedUserAsync("admin@example.com", "Admin User", "Password1!");
        var token = await LoginAsync("admin@example.com", "Password1!");

        var response = await PutUsersAsync(
            token,
            target.Id,
            new { name = "Updated User", email = "updated@example.com", password = "Password1!" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetInt64().Should().Be(target.Id);
        body.GetProperty("name").GetString().Should().Be("Updated User");
        body.GetProperty("email").GetString().Should().Be("updated@example.com");
    }

    [Fact]
    public async Task PutUsers_NotFound_ReturnsNotFound()
    {
        await SeedUserAsync("admin@example.com", "Admin User", "Password1!");
        var token = await LoginAsync("admin@example.com", "Password1!");

        var response = await PutUsersAsync(
            token,
            999,
            new { name = "Missing", email = "missing@example.com", password = "Password1!" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errorCode").GetString().Should().Be("Users.NotFound");
    }

    [Fact]
    public async Task PutUsers_DuplicateEmail_ReturnsEmailConflict()
    {
        var target = await SeedUserAsync("target@example.com", "Target User", "Password1!");
        await SeedUserAsync("existing@example.com", "Existing User");
        await SeedUserAsync("admin@example.com", "Admin User", "Password1!");
        var token = await LoginAsync("admin@example.com", "Password1!");

        var response = await PutUsersAsync(
            token,
            target.Id,
            new { name = "Duplicate", email = "existing@example.com", password = "Password1!" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errorCode").GetString().Should().Be("Users.EmailConflict");
    }

    private async Task<HttpResponseMessage> PutUsersAsync(string token, long id, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/users/{id}")
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

    private async Task<User> SeedUserAsync(
        string email,
        string name = "Admin User",
        string password = "Password1!")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var user = User.Create(name, email, hasher.Hash(password));
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }
}
