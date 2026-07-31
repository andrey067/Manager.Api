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
public class GetAllUsersEndpointTests(CreateUserWebApplicationFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync() => await factory.ResetStateAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetUsers_WithoutJwt_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUsers_WithJwt_ReturnsAllUsers()
    {
        var adminId = await SeedUserAsync("admin@example.com", "Admin User", "Password1!");
        await SeedUserAsync("other@example.com", "Other User");
        var token = factory.IssueToken(adminId, "admin@example.com");

        var response = await GetUsersAsync(token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.ValueKind.Should().Be(JsonValueKind.Array);
        body.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task GetUsers_AfterCreateUser_IncludesNewUser()
    {
        var adminId = await SeedUserAsync("getall-cache-admin@example.com", "Admin User", "Password1!");
        var token = factory.IssueToken(adminId, "getall-cache-admin@example.com");
        var newEmail = $"created-{Guid.NewGuid():N}@example.com";

        var before = await GetUsersAsync(token);
        before.StatusCode.Should().Be(HttpStatusCode.OK);
        var beforeEmails = (await before.Content.ReadFromJsonAsync<JsonElement>())
            .EnumerateArray()
            .Select(e => e.GetProperty("email").GetString())
            .ToHashSet();
        beforeEmails.Should().NotContain(newEmail);

        var createResponse = await PostUsersAsync(
            token,
            new { name = "Created User", email = newEmail, password = "Password1!" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var after = await GetUsersAsync(token);
        after.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterEmails = (await after.Content.ReadFromJsonAsync<JsonElement>())
            .EnumerateArray()
            .Select(e => e.GetProperty("email").GetString())
            .ToHashSet();
        afterEmails.Should().Contain(newEmail);
    }

    private async Task<HttpResponseMessage> GetUsersAsync(string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request);
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
