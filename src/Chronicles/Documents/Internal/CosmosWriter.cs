using System.Net;
using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents.Internal;

public class CosmosWriter<T> : IDocumentWriter<T>
    where T : IDocument
{
    private readonly Container container;
    private readonly ICosmosSerializer serializer;

    public CosmosWriter(
        ICosmosContainerProvider containerProvider,
        ICosmosSerializer serializer)
    {
        container = containerProvider.GetContainer<T>();
        this.serializer = serializer;
    }

    public IDocumentTransaction<T> CreateTransaction(
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
                options,
                cancellationToken: cancellationToken);

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
                var response = await container
                    .ReadItemAsync<T>(
                        documentId,
                        new PartitionKey(partitionKey),
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                var etag = response.ETag;
                var document = response.Resource;

                await updateDocument(document)
                    .ConfigureAwait(false);

                return await
                    ReplaceAsync(
                        document,
                        new ItemRequestOptions
                        {
                            IfMatchEtag = etag,
                        },
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

            var document = getDefaultDocument();
            if (string.IsNullOrEmpty(document.GetDocumentId()) ||
                string.IsNullOrEmpty(document.GetPartitionKey()))
            {
                throw new ArgumentException(
                    $"Default document needs {nameof(document.GetDocumentId)} " +
                    $"and {nameof(document.GetPartitionKey)} to return valid values.",
                    nameof(getDefaultDocument));
            }

            string? etag = null;
            bool documentExists;
            try
            {
                var response = await container
                    .ReadItemAsync<T>(
                        document.GetDocumentId(),
                        new PartitionKey(document.GetPartitionKey()),
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                etag = response.ETag;
                document = response.Resource;
                documentExists = true;
            }
            catch (CosmosException ex)
                 when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                documentExists = false;
            }

            await updateDocument(document)
                .ConfigureAwait(false);

            try
            {
                if (!documentExists)
                {
                    return await CreateAsync(
                       document,
                       null,
                       cancellationToken).ConfigureAwait(false);
                }

                return await ReplaceAsync(
                    document,
                    new ItemRequestOptions
                    {
                        IfMatchEtag = etag,
                    },
                    cancellationToken)
                    .ConfigureAwait(false);
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
