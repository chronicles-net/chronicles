using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents.Internal;

public class CosmosReader<T> : IDocumentReader<T>
{
    private readonly ICosmosContainerProvider containers;
    private readonly ICosmosLinqQuery linqQuery;

    public CosmosReader(
        ICosmosContainerProvider containerProvider,
        ICosmosLinqQuery linqQuery)
    {
        this.containers = containerProvider;
        this.linqQuery = linqQuery;
    }

    public QueryDefinition CreateQuery<TResult>(
        QueryExpression<T, TResult> query,
        string? storeName = null)
        => linqQuery.GetQueryDefinition(
            query.Invoke(
                containers
                    .GetContainer<T>(storeName)
                    .GetItemLinqQueryable<T>()));

    public async Task<TResult> ReadAsync<TResult>(
        string documentId,
        string partitionKey,
        ItemRequestOptions? options,
        string? storeName = null,
        CancellationToken cancellationToken = default)
        where TResult : T
        => await containers
            .GetContainer<T>(storeName)
            .ReadItemAsync<TResult>(
                documentId,
                new PartitionKey(partitionKey),
                options,
                cancellationToken: cancellationToken)
            .GetItemAsync()
            .ConfigureAwait(false);

    public IAsyncEnumerable<TResult> QueryAsync<TResult>(
        QueryDefinition query,
        string? partitionKey,
        QueryRequestOptions? options,
        string? storeName = null,
        CancellationToken cancellationToken = default)
        => containers
            .GetContainer<T>(storeName)
            .GetItemQueryIterator<TResult>(
                query,
                requestOptions: CreateOptions(options, partitionKey))
            .ToAsyncEnumerable(cancellationToken);

    public async Task<PagedResult<TResult>> PagedQueryAsync<TResult>(
        QueryDefinition query,
        string? partitionKey,
        int? maxItemCount,
        string? continuationToken,
        QueryRequestOptions? options,
        string? storeName = null,
        CancellationToken cancellationToken = default)
        => await containers
            .GetContainer<T>(storeName)
            .GetItemQueryIterator<TResult>(
                query,
                continuationToken,
                CreateOptions(options, partitionKey, maxItemCount))
            .ReadPageResultAsync(cancellationToken)
            .ConfigureAwait(false);

    private static QueryRequestOptions? CreateOptions(
        QueryRequestOptions? options,
        string? partitionKey,
        int? maxItemCount = null)
    {
        if (partitionKey == null && maxItemCount == null)
        {
            return options;
        }

        var requestOptions = options?.ShallowCopy() as QueryRequestOptions ?? new();
        if (partitionKey != null)
        {
            requestOptions.PartitionKey = new PartitionKey(partitionKey);
        }
        if (maxItemCount != null)
        {
            requestOptions.MaxItemCount = maxItemCount;
        }

        return requestOptions;
    }
}
