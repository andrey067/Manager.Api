using Manager.Api.Authentication;
using Manager.Api.Database;
using Manager.Api.Features.Users;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Manager.Vsa.Tests.Features.Users;

public sealed class CreateUserWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-signing-key-32-chars-minimum!!",
                ["Jwt:Issuer"] = "Manager.Api",
                ["Jwt:Audience"] = "Manager.Api",
                ["ConnectionStrings:ManagerAPIPostgres"] = "Host=localhost;Database=unused;Username=unused;Password=unused"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }

    public string IssueToken(long userId, string email)
    {
        using var scope = Services.CreateScope();
        var tokens = scope.ServiceProvider.GetRequiredService<ITokenService>();
        return tokens.CreateAccessToken(userId, email).Token;
    }

    public async Task ResetStateAsync()
    {
        using var scope = Services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<ApplicationDbContext>();
        var cache = sp.GetRequiredService<HybridCache>();

        var users = await db.Users.AsNoTracking().ToListAsync();
        foreach (var user in users)
        {
            await cache.RemoveAsync(UserCacheKeys.ById(user.Id));
            await cache.RemoveAsync(UserCacheKeys.ByEmail(user.Email));
        }

        await cache.RemoveAsync(UserCacheKeys.All);
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }
}
