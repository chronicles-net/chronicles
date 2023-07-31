using System.Text.Json.Nodes;
using Chronicles.Cosmos.Serialization;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace Chronicles.Cosmos.Internal;

public class CosmosReader<T> : ICosmosReader<T>
    where T : class
{
    private readonly Container container;
    private readonly IJsonCosmosSerializer serializer;

    public CosmosReader(
        IJsonCosmosSerializer serializer,
        ICosmosContainerProvider containerProvider)
    {
        container = containerProvider.GetContainer<T>();
        this.serializer = serializer;
    }

    public QueryDefinition CreateQuery<TResult>(
        Func<IQueryable<T>, IQueryable<TResult>> query)
        => query(container.GetItemLinqQueryable<T>()).ToQueryDefinition();

    public async Task<T?> FindAsync(
        string documentId,
        string partitionKey,
        ItemRequestOptions? options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await ReadAsync(
                documentId,
                partitionKey,
                options,
                cancellationToken)
                .ConfigureAwait(false);
        }
        catch (CosmosException ex)
         when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<T> ReadAsync(
        string documentId,
        string partitionKey,
        ItemRequestOptions? options,
        CancellationToken cancellationToken = default)
        => await container
            .ReadItemAsync<T>(
                documentId,
                new PartitionKey(partitionKey),
                options,
                cancellationToken: cancellationToken)
            .GetItemAsync()
            .ConfigureAwait(false);

    public IAsyncEnumerable<T> ReadAllAsync(
        string? partitionKey,
        QueryRequestOptions? options,
        CancellationToken cancellationToken = default)
        => container
            .GetItemLinqQueryable<object>(
                requestOptions: CreateOptions(options, partitionKey))
            .ToFeedIterator()
            .ToAsyncEnumerable<T>(serializer, cancellationToken);

    public IAsyncEnumerable<TResult> QueryAsync<TResult>(
        QueryDefinition query,
        string? partitionKey,
        QueryRequestOptions? options,
        CancellationToken cancellationToken = default)
        => container
            .GetItemQueryIterator<object>(
                query,
                requestOptions: CreateOptions(options, partitionKey))
            .ToAsyncEnumerable<TResult>(serializer, cancellationToken);

    public async Task<PagedResult<TResult>> PagedQueryAsync<TResult>(
        QueryDefinition query,
        string? partitionKey,
        QueryRequestOptions? options,
        int? maxItemCount,
        string? continuationToken = null,
        CancellationToken cancellationToken = default)
        => await container
            .GetItemQueryIterator<object>(
                query,
                continuationToken,
                CreateOptions(options, partitionKey, maxItemCount))
            .ReadPageResultAsync<TResult>(serializer, cancellationToken)
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
