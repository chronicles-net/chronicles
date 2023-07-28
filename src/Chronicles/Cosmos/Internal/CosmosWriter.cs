using System.Net;
using Chronicles.Cosmos.Serialization;
using Microsoft.Azure.Cosmos;

namespace Chronicles.Cosmos.Internal;

public class CosmosWriter<T> : ICosmosWriter<T>
    where T : class, ICosmosDocument
{
    private readonly Container container;
    private readonly IJsonCosmosSerializer serializer;
    private readonly ICosmosReader<T> reader;

    public CosmosWriter(
        ICosmosContainerProvider containerProvider,
        IJsonCosmosSerializer serializer,
        ICosmosReader<T> reader)
    {
        this.container = containerProvider.GetContainer<T>();
        this.serializer = serializer;
        this.reader = reader;
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

    public Task<T> UpdateAsync(
        string documentId,
        string partitionKey,
        Action<T> updateDocument,
        int retries = 0,
        CancellationToken cancellationToken = default)
        => UpdateAsync(
            documentId,
            partitionKey,
            MakeAsync(updateDocument),
            retries,
            cancellationToken);

    public async Task<T> UpdateAsync(
        string documentId,
        string partitionKey,
        Func<T, Task> updateDocument,
        int retries = 0,
        CancellationToken cancellationToken = default)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var document = await reader
                    .ReadAsync(
                        documentId,
                        partitionKey,
                        null,
                        cancellationToken)
                    .ConfigureAwait(false);

                await updateDocument(document)
                    .ConfigureAwait(false);

                return await
                    ReplaceAsync(
                        document,
                        null,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (CosmosException ex)
             when (ex.StatusCode == HttpStatusCode.PreconditionFailed)
            {
                if (--retries <= 0)
                {
                    throw;
                }
            }
        }
    }

    public Task<T> UpdateOrCreateAsync(
        Func<T> getDefaultDocument,
        Action<T> updateDocument,
        int retries = 0,
        CancellationToken cancellationToken = default)
        => UpdateOrCreateAsync(
            getDefaultDocument,
            MakeAsync(updateDocument),
            retries,
            cancellationToken);

    public async Task<T> UpdateOrCreateAsync(
        Func<T> getDefaultDocument,
        Func<T, Task> updateDocument,
        int retries = 0,
        CancellationToken cancellationToken = default)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var defaultDocument = getDefaultDocument();
                if (string.IsNullOrEmpty(defaultDocument.DocumentId) ||
                    string.IsNullOrEmpty(defaultDocument.PartitionKey))
                {
                    throw new ArgumentException(
                        $"Default document needs {nameof(defaultDocument.DocumentId)} " +
                        $"and {nameof(defaultDocument.PartitionKey)} to be set.",
                        nameof(getDefaultDocument));
                }

                var document = await reader
                    .FindAsync(
                        defaultDocument.DocumentId,
                        defaultDocument.PartitionKey,
                        cancellationToken)
                    .ConfigureAwait(false)
                    ?? defaultDocument;

                await updateDocument(document)
                    .ConfigureAwait(false);

                if (document.ETag is null)
                {
                    return await CreateAsync(
                        document,
                        null,
                        cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    return await ReplaceAsync(
                        document,
                        null,
                        cancellationToken).ConfigureAwait(false);
                }
            }
            catch (CosmosException ex)
             when (ex.StatusCode == HttpStatusCode.PreconditionFailed ||
                   ex.StatusCode == HttpStatusCode.Conflict)
            {
                if (--retries <= 0)
                {
                    throw;
                }
            }
        }
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

    private static Func<T, Task> MakeAsync(Action<T> action) => d =>
    {
        action.Invoke(d);
        return Task.CompletedTask;
    };
}
