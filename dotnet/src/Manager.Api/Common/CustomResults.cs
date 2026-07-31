using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Manager.Api.Common;

public static class CustomResults
{
    public static IResult Problem(Error error) => new ErrorProblemResult(error);

    internal static int MapStatusCode(ErrorType type) =>
        type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Problem => StatusCodes.Status400BadRequest,
            ErrorType.Failure => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError
        };

    private sealed class ErrorProblemResult(Error error) : IResult
    {
        public async Task ExecuteAsync(HttpContext httpContext)
        {
            var statusCode = MapStatusCode(error.Type);
            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Detail = error.Description,
            };
            problemDetails.Extensions["errorCode"] = error.Code;

            httpContext.Response.StatusCode = statusCode;
            httpContext.Response.ContentType = "application/problem+json";
            await httpContext.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
        }
    }
}
