using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Manager.Vsa.Tests.Infrastructure;

namespace Manager.Vsa.Tests.Health;

[Collection(PostgresCollection.Name)]
public class HealthEndpointTests
{
    private readonly HttpClient _client;

    public HealthEndpointTests(PostgresFixture postgres)
    {
        var factory = new CustomWebApplicationFactory(postgres);
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ReturnsOkWithStatus()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetString().Should().Be("ok");
    }
}
