using Api.Options;
using Api.Services;
using Microsoft.Extensions.Options;
using Moq;

namespace Api.Tests.Services;

public class TokenServiceTests
{
    [Fact]
    public void GenerateToken_ReturnsValidToken()
    {
        var jwtOptions = new JwtOptions { Key = "test_key_1234567890", Login = "test_user", HoursToExpire = 1 };
        var optionsMock = new Mock<IOptions<JwtOptions>>();
        optionsMock.Setup(o => o.Value).Returns(jwtOptions);

        var tokenService = new TokenService(optionsMock.Object);

        var token = tokenService.GenerateToken();

        Assert.NotNull(token);
        Assert.IsType<string>(token);
    }

    [Fact]
    public void GenerateToken_ThrowsException_WhenKeyIsNull()
    {
        var jwtOptions = new JwtOptions { Key = null, Login = "test_user", HoursToExpire = 1 };
        var optionsMock = new Mock<IOptions<JwtOptions>>();
        optionsMock.Setup(o => o.Value).Returns(jwtOptions);

        var tokenService = new TokenService(optionsMock.Object);

        Assert.Throws<ArgumentNullException>(() => tokenService.GenerateToken());
    }

    [Fact]
    public void GenerateToken_ThrowsException_WhenLoginIsNull()
    {
        var jwtOptions = new JwtOptions { Key = "test_key_1234567890", Login = null, HoursToExpire = 1 };
        var optionsMock = new Mock<IOptions<JwtOptions>>();
        optionsMock.Setup(o => o.Value).Returns(jwtOptions);

        var tokenService = new TokenService(optionsMock.Object);

        Assert.Throws<ArgumentNullException>(() => tokenService.GenerateToken());
    }

    [Fact]
    public void GenerateToken_ThrowsException_WhenHoursToExpireIsZero()
    {
        var jwtOptions = new JwtOptions { Key = "test_key_1234567890", Login = "test_user", HoursToExpire = 0 };
        var optionsMock = new Mock<IOptions<JwtOptions>>();
        optionsMock.Setup(o => o.Value).Returns(jwtOptions);

        var tokenService = new TokenService(optionsMock.Object);

        Assert.Throws<ArgumentException>(() => tokenService.GenerateToken());
    }
}