using Api.Interfaces;
using Api.Options;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Options;

namespace Api.Features.Authentication.Login;

public class LoginHandler(ITokenService tokenService, IOptions<JwtOptions> jwtOptions, ILogger<LoginHandler> logger)
    : IRequestHandler<LoginCommand, ErrorOr<LoginResponse>>
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;


    public  Task<ErrorOr<LoginResponse>> Handle(LoginCommand request, 
                                                CancellationToken cancellationToken)
    {
        logger.LogInformation("Login attempt for {Login}", request.Login);
        if (request.Login != _jwtOptions.Login || request.Password != _jwtOptions.Password)
            return Task.FromResult<ErrorOr<LoginResponse>>(Error.Unauthorized("Error: Invalid login or password"));
        
        var token = tokenService.GenerateToken();
        var tokenExpires = DateTime.UtcNow.AddHours(_jwtOptions.HoursToExpire);

        return Task.FromResult<ErrorOr<LoginResponse>>(new LoginResponse(token, tokenExpires));
    }
}