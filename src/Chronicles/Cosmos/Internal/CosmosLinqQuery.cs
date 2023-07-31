using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace Chronicles.Cosmos.Internal;

public class CosmosLinqQuery : ICosmosLinqQuery
{
    public FeedIterator<T> GetFeedIterator<T>(IQueryable<T> queryable)
        => queryable.ToFeedIterator();

    public QueryDefinition GetQueryDefinition<T>(IQueryable<T> queryable)
        => queryable.ToQueryDefinition();
}
