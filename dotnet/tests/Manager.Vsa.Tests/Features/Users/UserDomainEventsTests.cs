using FluentAssertions;
using Manager.Api.Features.Users;

namespace Manager.Vsa.Tests.Features.Users;

public class UserDomainEventsTests
{
    [Fact]
    public void UserCreatedDomainEvent_HasId()
    {
        var evt = new UserCreatedDomainEvent(7);
        evt.Id.Should().Be(7);
    }

    [Fact]
    public void UserUpdatedDomainEvent_HasId()
    {
        var evt = new UserUpdatedDomainEvent(8);
        evt.Id.Should().Be(8);
    }

    [Fact]
    public void UserRemovedDomainEvent_HasId()
    {
        var evt = new UserRemovedDomainEvent(9);
        evt.Id.Should().Be(9);
    }
}
