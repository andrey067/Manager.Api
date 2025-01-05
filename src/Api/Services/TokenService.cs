using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Api.Interfaces;
using Api.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Api.Services;

public class TokenService(IOptions<JwtOptions> jwtOptions) : ITokenService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public string GenerateToken()
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        if (_jwtOptions.Key is null)
            throw new ArgumentNullException(nameof(_jwtOptions.Key), "JwtOptions.Key is null");

        if (_jwtOptions.Login is null)
            throw new ArgumentNullException(nameof(_jwtOptions.Login), "JwtOptions.Login is null");

        var key = Encoding.ASCII.GetBytes(_jwtOptions.Key);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim(ClaimTypes.Name, _jwtOptions.Login!),
                new Claim(ClaimTypes.Role, "User")
            ]),
            Expires = DateTime.UtcNow.AddHours(_jwtOptions.HoursToExpire),

            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}