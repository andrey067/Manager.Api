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
public class RemoveUserEndpointTests : IAsyncLifetime
{
    private readonly CreateUserWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RemoveUserEndpointTests(PostgresFixture postgres)
    {
        _factory = new CreateUserWebApplicationFactory(postgres);
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync() => await _factory.ResetStateAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task DeleteUsers_WithoutJwt_ReturnsUnauthorized()
    {
        var response = await _client.DeleteAsync("/api/v1/users/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteUsers_WithJwt_RemovesUser()
    {
        var target = await SeedUserAsync("target@example.com", "Target User", "Password1!");
        await SeedUserAsync("admin@example.com", "Admin User", "Password1!");
        var token = await LoginAsync("admin@example.com", "Password1!");

        var response = await DeleteUsersAsync(token, target.Id);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Content.Headers.ContentLength.Should().Be(0);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        (await db.Users.AnyAsync(u => u.Id == target.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteUsers_NotFound_ReturnsNotFound()
    {
        await SeedUserAsync("admin@example.com", "Admin User", "Password1!");
        var token = await LoginAsync("admin@example.com", "Password1!");

        var response = await DeleteUsersAsync(token, 999);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errorCode").GetString().Should().Be("Users.NotFound");
    }

    private async Task<HttpResponseMessage> DeleteUsersAsync(string token, long id)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/users/{id}");
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
