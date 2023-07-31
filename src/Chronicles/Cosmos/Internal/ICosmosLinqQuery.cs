using Microsoft.Azure.Cosmos;

namespace Chronicles.Cosmos.Internal;

public interface ICosmosLinqQuery
{
    QueryDefinition GetQueryDefinition<T>(IQueryable<T> queryable);

    FeedIterator<T> GetFeedIterator<T>(IQueryable<T> queryable);
}
