using System.Net;
using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents.Internal;

internal class CosmosWriter<T> : IDocumentWriter<T>
    where T : IDocument
{
    private readonly ICosmosContainerProvider containers;

    public CosmosWriter(
        ICosmosContainerProvider containerProvider)
    {
        this.containers = containerProvider;
    }

    public IDocumentTransaction<T> CreateTransaction(
        string partitionKey,
        string? storeName = null)
        => new CosmosTransaction<T>(
            containers
                .GetContainer<T>(storeName)
                .CreateTransactionalBatch(
                    new PartitionKey(partitionKey)));

    public Task<TIn> CreateAsync<TIn>(
        TIn document,
        ItemRequestOptions? options,
        string? storeName = null,
        CancellationToken cancellationToken = default)
        where TIn : T
        => containers
            .GetContainer<T>(storeName)
            .CreateItemAsync(
                document,
                new PartitionKey(document.GetPartitionKey()),
                options,
                cancellationToken)
            .GetItemOrDefaultAsync(document);

    public Task<TIn> WriteAsync<TIn>(
        TIn document,
        ItemRequestOptions? options,
        string? storeName = null,
        CancellationToken cancellationToken = default)
        where TIn : T
        => containers
            .GetContainer<T>(storeName)
            .UpsertItemAsync(
                document,
                new PartitionKey(document.GetPartitionKey()),
                options,
                cancellationToken)
            .GetItemOrDefaultAsync(document);

    public Task<TIn> ReplaceAsync<TIn>(
        TIn document,
        ItemRequestOptions? options,
        string? storeName = null,
        CancellationToken cancellationToken = default)
        where TIn : T
        => containers
            .GetContainer<T>(storeName)
            .ReplaceItemAsync(
                document,
                document.GetDocumentId(),
                new PartitionKey(document.GetPartitionKey()),
                options,
                cancellationToken)
            .GetItemOrDefaultAsync(document);

    public Task DeleteAsync(
        string documentId,
        string partitionKey,
        ItemRequestOptions? options,
        string? storeName = null,
        CancellationToken cancellationToken = default)
        => containers
            .GetContainer<T>(storeName)
            .DeleteItemAsync<T>(
                documentId,
                new PartitionKey(partitionKey),
                options,
                cancellationToken: cancellationToken);

    public async Task DeletePartitionAsync(
        string partitionKey,
        ItemRequestOptions? options,
        string? storeName = null,
        CancellationToken cancellationToken = default)
    {
        using var response = await containers
            .GetContainer<T>(storeName)
            .DeleteAllItemsByPartitionKeyStreamAsync(
                new PartitionKey(partitionKey),
                options,
                cancellationToken: cancellationToken);
        if (!response.IsSuccessStatusCode) // Reason the required feature is not enabled on the cosmos account.
        {
            throw new InvalidOperationException(
                $"Failed to delete partition. StatusCode: {response.StatusCode}, ActivityId: {response.Headers.ActivityId}, Reason: {response.ErrorMessage}");
        }
    }

    public async Task<T> UpdateAsync(
        string documentId,
        string partitionKey,
        Func<T, Task<T>> updateDocument,
        int retries = 0,
        string? storeName = null,
        CancellationToken cancellationToken = default)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var response = await containers
                    .GetContainer<T>(storeName)
                    .ReadItemAsync<T>(
                        documentId,
                        new PartitionKey(partitionKey),
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                var etag = response.ETag;
                var document = response.Resource;

                document = await updateDocument(document)
                    .ConfigureAwait(false);

                return await
                    ReplaceAsync(
                        document,
                        new ItemRequestOptions
                        {
                            IfMatchEtag = etag,
                        },
                        storeName,
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

    public async Task<T?> ConditionalUpdateAsync(
        string documentId,
        string partitionKey,
        Func<T, bool> condition,
        Func<T, Task<T>> updateDocument,
        string? storeName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await containers
                .GetContainer<T>(storeName)
                .ReadItemAsync<T>(
                    documentId,
                    new PartitionKey(partitionKey),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var etag = response.ETag;
            var document = response.Resource;

            if (!condition(document))
            {
                return default;
            }

            document = await updateDocument(document)
                .ConfigureAwait(false);

            return await
                ReplaceAsync(
                    document,
                    new ItemRequestOptions
                    {
                        IfMatchEtag = etag,
                    },
                    storeName,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (CosmosException ex)
            when (ex.StatusCode == HttpStatusCode.PreconditionFailed
               || ex.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }
    }

    public async Task<T> UpdateOrCreateAsync(
        Func<T> getDefaultDocument,
        Func<T, Task<T>> updateDocument,
        int retries = 0,
        string? storeName = null,
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
                var response = await containers
                    .GetContainer<T>(storeName)
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

            document = await updateDocument(document)
                .ConfigureAwait(false);

            try
            {
                if (!documentExists)
                {
                    return await CreateAsync(
                       document,
                       null,
                       storeName,
                       cancellationToken).ConfigureAwait(false);
                }

                return await ReplaceAsync(
                    document,
                    new ItemRequestOptions
                    {
                        IfMatchEtag = etag,
                    },
                    storeName,
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
