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
                new PartitionKey(document.GetPartitionKey()),
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
                new PartitionKey(document.GetPartitionKey()),
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
                document.GetDocumentId(),
                new PartitionKey(document.GetPartitionKey()),
                options,
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
                if (string.IsNullOrEmpty(defaultDocument.GetDocumentId()) ||
                    string.IsNullOrEmpty(defaultDocument.GetPartitionKey()))
                {
                    throw new ArgumentException(
                        $"Default document needs {nameof(defaultDocument.GetDocumentId)} " +
                        $"and {nameof(defaultDocument.GetPartitionKey)} to return valid values.",
                        nameof(getDefaultDocument));
                }

                var document = await reader
                    .FindAsync(
                        defaultDocument.GetDocumentId(),
                        defaultDocument.GetPartitionKey(),
                        cancellationToken)
                    .ConfigureAwait(false);

                bool shouldCreate = document == null;

                document ??= defaultDocument;

                await updateDocument(document)
                    .ConfigureAwait(false);

                if (shouldCreate)
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
}
