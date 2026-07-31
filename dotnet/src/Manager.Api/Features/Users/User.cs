using Manager.Api.Common.Domain;

namespace Manager.Api.Features.Users;

public sealed class User : Entity
{
    public string Name { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string Password { get; private set; } = null!;
    public string? RefreshTokenHash { get; private set; }
    public DateTime? RefreshTokenExpiresAt { get; private set; }

    private User()
    {
    }

    public static User Create(string name, string email, string password) =>
        new()
        {
            Name = name,
            Email = email,
            Password = password
        };

    public void SetName(string name) => Name = name;

    public void SetEmail(string email) => Email = email;

    public void SetPassword(string password) => Password = password;

    public void SetRefreshToken(string? refreshTokenHash, DateTime? expiresAt)
    {
        RefreshTokenHash = refreshTokenHash;
        RefreshTokenExpiresAt = expiresAt;
    }

    public void ClearRefreshToken()
    {
        RefreshTokenHash = null;
        RefreshTokenExpiresAt = null;
    }
}
