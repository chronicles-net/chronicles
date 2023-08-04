using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents.Internal;

public interface ICosmosLinqQuery
{
    QueryDefinition GetQueryDefinition<T>(IQueryable<T> queryable);

    FeedIterator<T> GetFeedIterator<T>(IQueryable<T> queryable);
}
