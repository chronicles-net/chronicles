using Microsoft.Azure.Cosmos;

namespace Chronicles.Cosmos.Testing;

public class FakeQueryDefinition<T> : QueryDefinition
{
    public FakeQueryDefinition(
        Func<IQueryable<T>, IQueryable> query)
        : base($"Query{Guid.NewGuid()}")
    {
        this.LinqQuery = query;
    }

    public Func<IQueryable<T>, IQueryable> LinqQuery { get; }
}
