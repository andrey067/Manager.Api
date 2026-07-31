using EscNet.Hashers.Interfaces.Algorithms;

namespace Manager.Api.Authentication;

public sealed class Argon2PasswordHasher(IArgon2IdHasher hasher) : IPasswordHasher
{
    public string Hash(string password) => hasher.Hash(password);

    public bool Verify(string password, string hash) => hasher.VerifyHashedText(password, hash);
}
