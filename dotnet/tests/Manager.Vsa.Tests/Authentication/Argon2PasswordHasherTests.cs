using EscNet.Hashers.Interfaces.Algorithms;
using EscNet.IoC.Hashers;
using FluentAssertions;
using Isopoh.Cryptography.Argon2;
using Manager.Api.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Manager.Vsa.Tests.Authentication;

public class Argon2PasswordHasherTests
{
    private static IPasswordHasher CreateHasher()
    {
        var services = new ServiceCollection();
        var config = new Argon2Config
        {
            Type = Argon2Type.DataIndependentAddressing,
            Version = Argon2Version.Nineteen,
            TimeCost = 1,
            MemoryCost = 8,
            Lanes = 1,
            Threads = 1,
            Salt = new byte[16],
            HashLength = 16
        };
        services.AddArgon2IdHasher(config);
        services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();
        return services.BuildServiceProvider().GetRequiredService<IPasswordHasher>();
    }

    [Fact]
    public void Hash_And_Verify_Roundtrip()
    {
        var hasher = CreateHasher();
        var hashed = hasher.Hash("Secret123!");
        hashed.Should().StartWith("$argon2");
        hasher.Verify("Secret123!", hashed).Should().BeTrue();
        hasher.Verify("WrongPassword", hashed).Should().BeFalse();
    }
}
