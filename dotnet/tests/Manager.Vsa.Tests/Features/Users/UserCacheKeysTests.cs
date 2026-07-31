using FluentAssertions;
using Manager.Api.Features.Users;

namespace Manager.Vsa.Tests.Features.Users;

public class UserCacheKeysTests
{
    [Fact]
    public void All_HasExpectedValue() => UserCacheKeys.All.Should().Be("users:all");

    [Fact]
    public void ById_FormatsKey() => UserCacheKeys.ById(42).Should().Be("users:42");

    [Fact]
    public void ByEmail_FormatsKeyWithLowerInvariantEmail()
    {
        UserCacheKeys.ByEmail("User@Example.COM").Should().Be("users:email:user@example.com");
    }
}
