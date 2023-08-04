using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace Chronicles.Documents.Internal;

public class CosmosLinqQuery : ICosmosLinqQuery
{
    public QueryDefinition GetQueryDefinition<T>(IQueryable<T> queryable)
        => queryable.ToQueryDefinition();

    public FeedIterator<T> GetFeedIterator<T>(IQueryable<T> queryable)
        => queryable.ToFeedIterator();
}
