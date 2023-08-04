using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents.Internal;

public class CosmosReader<T> : IDocumentReader<T>
{
    private readonly Container container;
    private readonly ICosmosLinqQuery linqQuery;
    private readonly ICosmosSerializer serializer;

    public CosmosReader(
        ICosmosContainerProvider containerProvider,
        ICosmosSerializerProvider serializerProvider,
        ICosmosLinqQuery linqQuery)
    {
        container = containerProvider.GetContainer<T>();
        serializer = serializerProvider.GetSerializer<T>();

        this.linqQuery = linqQuery;
    }

    public QueryDefinition CreateQuery<TResult>(
        QueryExpression<T, TResult> query)
        => linqQuery.GetQueryDefinition(
            query.Invoke(
                container.GetItemLinqQueryable<T>(linqSerializerOptions: new CosmosLinqSerializerOptions
                {
                    PropertyNamingPolicy = serializer.PropertyNamingPolicy,
                })));

    public async Task<TResult> ReadAsync<TResult>(
        string documentId,
        string partitionKey,
        ItemRequestOptions? options,
        CancellationToken cancellationToken = default)
        where TResult : T
        => await container
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
        CancellationToken cancellationToken = default)
        => container
            .GetItemQueryIterator<TResult>(
                query,
                requestOptions: CreateOptions(options, partitionKey))
            .ToAsyncEnumerable(cancellationToken);

    public async Task<PagedResult<TResult>> PagedQueryAsync<TResult>(
        QueryDefinition query,
        string? partitionKey,
        QueryRequestOptions? options,
        int? maxItemCount,
        string? continuationToken = null,
        CancellationToken cancellationToken = default)
        => await container
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
