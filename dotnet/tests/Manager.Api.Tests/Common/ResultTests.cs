using FluentAssertions;
using Manager.Api.Common;

namespace Manager.Api.Tests.Common;

public class ResultTests
{
    [Fact]
    public void Success_IsSuccess_And_Match_Calls_OnSuccess()
    {
        var result = Result.Success();
        result.IsSuccess.Should().BeTrue();
        var value = result.Match(() => 1, _ => 0);
        value.Should().Be(1);
    }

    [Fact]
    public void Failure_IsFailure_And_Match_Calls_OnFailure()
    {
        var error = Error.NotFound("Users.NotFound", "User not found");
        var result = Result.Failure(error);
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Users.NotFound");
        var value = result.Match(() => 1, e => 0);
        value.Should().Be(0);
    }

    [Fact]
    public void ResultT_Success_Exposes_Value()
    {
        var result = Result.Success(42);
        result.Value.Should().Be(42);
        result.Match(v => v, _ => -1).Should().Be(42);
    }
}
