using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Text;
using FluentValidation;
using Manager.Api.Authentication;
using Manager.Api.Common;
using Manager.Api.Common.Messaging;
using Manager.Api.Database;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsEnvironment("Testing"))
    ValidateJwtKey(builder.Configuration);

builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
builder.Services.AddHybridCache();

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("ManagerAPIPostgres")));
}

builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly, includeInternalTypes: true);

builder.Services.Scan(scan => scan
    .FromAssemblyOf<Program>()
    .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<>)), publicOnly: false)
    .AsImplementedInterfaces()
    .WithScopedLifetime());

builder.Services.Scan(scan => scan
    .FromAssemblyOf<Program>()
    .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<,>)), publicOnly: false)
    .AsImplementedInterfaces()
    .WithScopedLifetime());

builder.Services.Scan(scan => scan
    .FromAssemblyOf<Program>()
    .AddClasses(c => c.AssignableTo(typeof(IQueryHandler<,>)), publicOnly: false)
    .AsImplementedInterfaces()
    .WithScopedLifetime());

builder.Services.AddEndpoints(Assembly.GetExecutingAssembly());

builder.Services.AddAuthenticationInfrastructure(builder.Configuration);
AddJwt(builder.Services, builder.Configuration);

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapEndpoints();

app.Run();

static void ValidateJwtKey(IConfiguration configuration)
{
    var secretKey = configuration["Jwt:Key"];
    if (string.IsNullOrWhiteSpace(secretKey) || secretKey.Length < 32)
        throw new InvalidOperationException(
            "Jwt:Key must be configured with at least 32 characters via User Secrets or environment variables.");
}

static void AddJwt(IServiceCollection services, IConfiguration configuration)
{
    services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer();

    services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
        .Configure<IConfiguration>((options, config) =>
        {
            var secretKey = config["Jwt:Key"]!;
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            var issuer = config["Jwt:Issuer"];
            var audience = config["Jwt:Audience"];

            options.MapInboundClaims = false;
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                ValidateIssuer = !string.IsNullOrWhiteSpace(issuer),
                ValidIssuer = issuer,
                ValidateAudience = !string.IsNullOrWhiteSpace(audience),
                ValidAudience = audience,
                ClockSkew = TimeSpan.FromMinutes(1),
                NameClaimType = JwtRegisteredClaimNames.Sub
            };
        });

    services.AddAuthorization();
}

public partial class Program;
