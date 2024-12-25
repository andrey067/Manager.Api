namespace Api.Common;

public abstract class AggregateRootId<TId>
{
    public abstract TId Value { get; protected set; }
}