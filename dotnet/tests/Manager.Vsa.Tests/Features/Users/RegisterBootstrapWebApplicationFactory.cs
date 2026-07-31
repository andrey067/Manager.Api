using Manager.Api.Database;
using Manager.Vsa.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Manager.Vsa.Tests.Features.Users;

public sealed class RegisterBootstrapWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public RegisterBootstrapWebApplicationFactory(PostgresFixture postgres) =>
        _connectionString = postgres.ConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-signing-key-32-chars-minimum!!",
                ["ConnectionStrings:ManagerAPIPostgres"] = _connectionString
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(_connectionString));
        });
    }
}
