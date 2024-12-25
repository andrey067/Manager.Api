using Api.Features.Authentication;
using Api.Features.Authentication.Login;
using Api.Interfaces;
using Api.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Api.Tests.Features.Authentication;

public class LoginHandlerTests
{
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IOptions<JwtOptions>> _jwtOptionsMock;
    private readonly Mock<ILogger<LoginHandler>> _loggerMock;
    private readonly LoginHandler _handler;

    public LoginHandlerTests()
    {
        _tokenServiceMock = new Mock<ITokenService>();
        _jwtOptionsMock = new Mock<IOptions<JwtOptions>>();
        _loggerMock = new Mock<ILogger<LoginHandler>>();

        var jwtOptions = new JwtOptions
        {
            Login = "testLogin",
            Password = "testPassword",
            HoursToExpire = 1
        };

        _jwtOptionsMock.Setup(x => x.Value).Returns(jwtOptions);

        _handler = new LoginHandler(_tokenServiceMock.Object, _jwtOptionsMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenLoginOrPasswordIsInvalid()
    {
        // Arrange
        var command = new LoginCommand("invalidLogin", "invalidPassword");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal("Error: Invalid login or password", result.FirstError.Code);
    }

    [Fact]
    public async Task Handle_ShouldReturnToken_WhenLoginAndPasswordAreValid()
    {
        // Arrange
        var command = new LoginCommand("testLogin", "testPassword");
        var token = "testToken";
        _tokenServiceMock.Setup(x => x.GenerateToken()).Returns(token);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(token, result.Value.Token);
        Assert.Equal(DateTime.UtcNow.AddHours(1).Hour, result.Value.TokenExpires.Hour);
    }
}