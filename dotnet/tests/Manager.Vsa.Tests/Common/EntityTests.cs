using FluentAssertions;
using Manager.Api.Common.Domain;

namespace Manager.Vsa.Tests.Common;

file sealed record TestEvent(long Id) : IDomainEvent;
file sealed class TestEntity : Entity;

public class EntityTests
{
    [Fact]
    public void Raise_Adds_DomainEvent_Clear_Removes()
    {
        var entity = new TestEntity();
        entity.Raise(new TestEvent(1));
        entity.DomainEvents.Should().ContainSingle();
        entity.ClearDomainEvents();
        entity.DomainEvents.Should().BeEmpty();
    }
}
