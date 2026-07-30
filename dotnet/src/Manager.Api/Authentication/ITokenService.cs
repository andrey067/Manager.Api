namespace Manager.Api.Authentication;

public interface ITokenService
{
    (string Token, DateTime Expires) CreateAccessToken(long userId, string email);
    string CreateRefreshToken();
    string HashRefreshToken(string refreshToken);
    DateTime GetAccessExpiry();
    DateTime GetRefreshExpiry();
    bool TryValidateAccessToken(string token, out long userId);
}
