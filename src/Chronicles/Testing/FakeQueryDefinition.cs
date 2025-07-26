using Microsoft.Azure.Cosmos;

namespace Chronicles.Testing;

public class FakeQueryDefinition<T> : QueryDefinition
{
    public FakeQueryDefinition(
        Func<IQueryable<T>, IQueryable> query)
        : base($"Query{Guid.NewGuid()}")
        => LinqQuery = query;

    public Func<IQueryable<T>, IQueryable> LinqQuery { get; }
}
