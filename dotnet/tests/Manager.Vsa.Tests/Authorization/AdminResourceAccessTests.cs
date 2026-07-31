using FluentAssertions;
using Manager.Api.Authorization;

namespace Manager.Vsa.Tests.Authorization;

public class AdminResourceAccessTests
{
    [Fact]
    public void Users_IsAdminManaged_DoesNotRequireResourceOwnership()
    {
        AdminResourceAccess.IsAdminManagedFeature("Users").Should().BeTrue();
        AdminResourceAccess.RequiresResourceOwnership("Users").Should().BeFalse();
    }

    [Fact]
    public void UnknownFeature_DefaultsToOwnershipRequired()
    {
        AdminResourceAccess.RequiresResourceOwnership("Orders").Should().BeTrue();
    }
}
