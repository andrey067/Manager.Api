using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Manager.Api.Common;
using Microsoft.IdentityModel.Tokens;

namespace Manager.Api.Authentication;

public sealed class JwtTokenService(IConfiguration configuration, IDateTimeProvider clock) : ITokenService
{
    public (string Token, DateTime Expires) CreateAccessToken(long userId, string email)
    {
        var now = clock.UtcNow;
        var expires = now.AddHours(int.Parse(configuration["Jwt:HoursToExpire"] ?? "1"));
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(ClaimTypes.Role, "User")
            ]),
            NotBefore = now,
            IssuedAt = now,
            Expires = expires,
            Issuer = GetIssuer(),
            Audience = GetAudience(),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(GetSigningKey()),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return (tokenHandler.WriteToken(token), expires);
    }

    public string CreateRefreshToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public string HashRefreshToken(string refreshToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(bytes);
    }

    public DateTime GetAccessExpiry()
    {
        var hours = int.Parse(configuration["Jwt:HoursToExpire"] ?? "1");
        return clock.UtcNow.AddHours(hours);
    }

    public DateTime GetRefreshExpiry()
    {
        var days = int.Parse(configuration["Jwt:RefreshDaysToExpire"] ?? "7");
        return clock.UtcNow.AddDays(days);
    }

    public bool TryValidateAccessToken(string token, out long userId)
    {
        userId = 0;
        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var principal = tokenHandler.ValidateToken(
                token,
                new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(GetSigningKey()),
                    ValidateIssuer = !string.IsNullOrWhiteSpace(GetIssuer()),
                    ValidIssuer = GetIssuer(),
                    ValidateAudience = !string.IsNullOrWhiteSpace(GetAudience()),
                    ValidAudience = GetAudience(),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    LifetimeValidator = (notBefore, expires, _, parameters) =>
                    {
                        var now = clock.UtcNow;
                        if (notBefore.HasValue && now < notBefore.Value.Subtract(parameters.ClockSkew))
                            return false;
                        if (expires.HasValue && expires.Value.Add(parameters.ClockSkew) < now)
                            return false;
                        return true;
                    }
                },
                out _);

            var idClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return idClaim is not null && long.TryParse(idClaim, out userId);
        }
        catch (SecurityTokenException)
        {
            return false;
        }
    }

    private byte[] GetSigningKey()
    {
        var secretKey = configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(secretKey) || secretKey.Length < 32)
            throw new InvalidOperationException(
                "Jwt:Key must be configured with at least 32 characters via User Secrets or environment variables.");

        return Encoding.UTF8.GetBytes(secretKey);
    }

    private string? GetIssuer() => configuration["Jwt:Issuer"];

    private string? GetAudience() => configuration["Jwt:Audience"];
}
