using Api.Common;

namespace Api.Database.ValueObjects;

public sealed class UserId : AggregateRootId<long>
{
    public override long Value { get; protected set; }
    
    private UserId(long value)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(nameof(value), "User id must be greater than zero.");

        Value = value;
    }
    
    public static UserId From(long value) => new(value);
    
#pragma warning disable CS0628 
    protected UserId() { }
#pragma warning restore CS0628 
}