using System.Net;
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
public class RegisterBootstrapEndpointTests : IAsyncLifetime
{
    private readonly RegisterBootstrapWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RegisterBootstrapEndpointTests(PostgresFixture postgres)
    {
        _factory = new RegisterBootstrapWebApplicationFactory(postgres);
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync() => await ClearUsersAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task PostBootstrap_InvalidName_ReturnsValidationError()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/users/bootstrap",
            new { name = "A", email = "user@example.com", password = "Password1!" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errorCode").GetString().Should().Be("Validation.Error");
    }

    [Fact]
    public async Task PostBootstrap_WhenUsersExist_ReturnsBootstrapNotAllowed()
    {
        await SeedUserAsync("existing@example.com");

        var response = await _client.PostAsJsonAsync(
            "/api/v1/users/bootstrap",
            new { name = "New User", email = "new@example.com", password = "Password1!" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errorCode").GetString().Should().Be("Users.BootstrapNotAllowed");
    }

    [Fact]
    public async Task PostBootstrap_WhenEmpty_CreatesUser()
    {
        await ClearUsersAsync();

        var response = await _client.PostAsJsonAsync(
            "/api/v1/users/bootstrap",
            new { name = "Bootstrap Admin", email = "bootstrap@example.com", password = "Password1!" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetInt64().Should().BeGreaterThan(0);
        body.GetProperty("name").GetString().Should().Be("Bootstrap Admin");
        body.GetProperty("email").GetString().Should().Be("bootstrap@example.com");
    }

    private async Task SeedUserAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        db.Users.Add(User.Create("Existing User", email, hasher.Hash("Password1!")));
        await db.SaveChangesAsync();
    }

    private async Task ClearUsersAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Users.ExecuteDeleteAsync();
    }
}
