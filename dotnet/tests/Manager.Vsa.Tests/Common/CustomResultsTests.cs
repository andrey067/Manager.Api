using FluentAssertions;
using Manager.Api.Common;
using Microsoft.AspNetCore.Http;

namespace Manager.Vsa.Tests.Common;

public class CustomResultsTests
{
    [Theory]
    [InlineData(ErrorType.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorType.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(ErrorType.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorType.Problem, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorType.Failure, StatusCodes.Status500InternalServerError)]
    public async Task Problem_Maps_Status_And_ErrorCode(ErrorType type, int status)
    {
        var error = new Error("Users.Test", "detail", type);
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        await CustomResults.Problem(error).ExecuteAsync(httpContext);
        httpContext.Response.StatusCode.Should().Be(status);
    }
}
