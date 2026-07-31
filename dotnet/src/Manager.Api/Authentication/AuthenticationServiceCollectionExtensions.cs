using System.Security.Cryptography;
using System.Text;
using EscNet.IoC.Hashers;
using Isopoh.Cryptography.Argon2;

namespace Manager.Api.Authentication;

public static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddAuthenticationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        AddArgon2Hasher(services, configuration);

        services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, HttpUserContext>();

        return services;
    }

    private static void AddArgon2Hasher(IServiceCollection services, IConfiguration configuration)
    {
        var saltConfig = configuration["Hash:Salt"];
        byte[] salt;
        if (string.IsNullOrWhiteSpace(saltConfig))
        {
            salt = RandomNumberGenerator.GetBytes(16);
        }
        else
        {
            salt = Encoding.UTF8.GetBytes(saltConfig);
        }

        var config = new Argon2Config
        {
            Type = Argon2Type.DataIndependentAddressing,
            Version = Argon2Version.Nineteen,
            TimeCost = int.Parse(configuration["Hash:TimeCost"] ?? "10"),
            MemoryCost = int.Parse(configuration["Hash:MemoryCost"] ?? "32768"),
            Lanes = int.Parse(configuration["Hash:Lanes"] ?? "5"),
            Threads = Environment.ProcessorCount,
            Salt = salt,
            HashLength = int.Parse(configuration["Hash:HashLength"] ?? "32")
        };

        services.AddArgon2IdHasher(config);
    }
}
