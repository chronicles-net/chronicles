using System.Net;
using Chronicles.Cosmos.Serialization;
using Microsoft.Azure.Cosmos;

namespace Chronicles.Cosmos.Internal;

public class CosmosWriter<T> : ICosmosWriter<T>
    where T : ICosmosDocument
{
    private readonly Container container;
    private readonly IJsonCosmosSerializer serializer;

    public CosmosWriter(
        ICosmosContainerProvider containerProvider,
        IJsonCosmosSerializer serializer)
    {
        this.container = containerProvider.GetContainer<T>();
        this.serializer = serializer;
    }

    public ICosmosTransaction<T> CreateTransaction(
        string partitionKey)
        => new CosmosTransaction<T>(
            container.CreateTransactionalBatch(
                new PartitionKey(partitionKey)));

    public Task<T> CreateAsync(
        T document,
        ItemRequestOptions? options,
        CancellationToken cancellationToken = default)
        => container
            .CreateItemAsync<object>(
                document,
                new PartitionKey(document.PartitionKey),
                options,
                cancellationToken)
            .GetItemOrDefaultAsync(serializer, document);

    public Task<T> WriteAsync(
        T document,
        ItemRequestOptions? options,
        CancellationToken cancellationToken = default)
        => container
            .UpsertItemAsync<object>(
                document,
                new PartitionKey(document.PartitionKey),
                options,
                cancellationToken)
            .GetItemOrDefaultAsync(serializer, document);

    public Task<T> ReplaceAsync(
        T document,
        ItemRequestOptions? options,
        CancellationToken cancellationToken = default)
        => container
            .ReplaceItemAsync<object>(
                document,
                document.DocumentId,
                new PartitionKey(document.PartitionKey),
                CreateOptions(options, document.ETag),
                cancellationToken)
            .GetItemOrDefaultAsync(serializer, document);

    public Task DeleteAsync(
        string documentId,
        string partitionKey,
        ItemRequestOptions? options,
        CancellationToken cancellationToken = default)
        => container
            .DeleteItemAsync<object>(
                documentId,
                new PartitionKey(partitionKey),
                cancellationToken: cancellationToken);

    public async Task<bool> TryDeleteAsync(
        string documentId,
        string partitionKey,
        ItemRequestOptions? options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await container
                .DeleteItemAsync<object>(
                    documentId,
                    new PartitionKey(partitionKey),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (CosmosException ex)
         when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        return true;
    }

    private static ItemRequestOptions? CreateOptions(
        ItemRequestOptions? options,
        string? ifMatchEtag)
    {
        var requestOptions = options;
        if (ifMatchEtag != null)
        {
            requestOptions = requestOptions?.ShallowCopy() as ItemRequestOptions ?? new();
            requestOptions.IfMatchEtag = ifMatchEtag;
        }

        return requestOptions;
    }
}
