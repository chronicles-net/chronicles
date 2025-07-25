using System.Net;
using System.Text.Json;
using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents.Testing;

/// <summary>
/// Represents a fake <see cref="IDocumentWriter{T}"/> that can be
/// used when unit testing client code.
/// </summary>
/// <typeparam name="T">
/// The type of <see cref="IDocument"/>
/// to be read by this reader.
/// </typeparam>
public class FakeDocumentWriter<T> :
    IDocumentWriter<T>
    where T : IDocument
{
    private readonly IFakeDocumentStoreProvider storeProvider;

    public FakeDocumentWriter(
        IFakeDocumentStoreProvider storeProvider)
        => this.storeProvider = storeProvider;

    ///// <summary>
    ///// Gets or sets the list of documents to be modified by the fake writer.
    ///// </summary>
    //public IList<T> Documents { get; set; } = [];

    public IDocumentTransaction<T> CreateTransaction(
        string partitionKey,
        string? storeName = null)
        => new FakeDocumentTransaction<T>(
            storeProvider
                .GetStore(storeName)
                .GetContainer<T>()
                .GetOrCreatePartition(partitionKey));

    public virtual async Task<TIn> CreateAsync<TIn>(
        TIn document,
        ItemRequestOptions? options,
        string? storeName = null,
        CancellationToken cancellationToken = default)
        where TIn : T
    {
        var newDocument = document.DeepClone(GetSerializerOptions(storeName));
        var success = await storeProvider
            .GetStore(storeName)
            .GetContainer<T>()
            .GetOrCreatePartition(newDocument.GetPartitionKey())
            .CreateDocument(
                newDocument.GetDocumentId(),
                newDocument);
        if (!success)
        {
            throw new CosmosException(
                $"Document already exists.",
                HttpStatusCode.Conflict,
                0,
                string.Empty,
                0);
        }

        return newDocument;
    }

    public virtual async Task<TIn> WriteAsync<TIn>(
        TIn document,
        ItemRequestOptions? options,
        string? storeName = null,
        CancellationToken cancellationToken = default)
        where TIn : T
    {
        var newDocument = document.DeepClone(GetSerializerOptions(storeName));

        await storeProvider
            .GetStore(storeName)
            .GetContainer<T>()
            .GetOrCreatePartition(newDocument.GetPartitionKey())
            .UpsertDocument(
                newDocument.GetDocumentId(),
                newDocument);

        return newDocument;
    }

    public virtual async Task<TIn> ReplaceAsync<TIn>(
        TIn document,
        ItemRequestOptions? options,
        string? storeName = null,
        CancellationToken cancellationToken = default)
        where TIn : T
    {
        var newDocument = document.DeepClone(GetSerializerOptions(storeName));

        var success = await storeProvider
            .GetStore(storeName)
            .GetContainer<T>()
            .GetOrCreatePartition(newDocument.GetPartitionKey())
            .ReplaceDocument(
                newDocument.GetDocumentId(),
                newDocument);
        if (!success)
        {
            throw new CosmosException(
                $"Document not found. " +
                $"Id: {newDocument.GetDocumentId()}. " +
                $"PartitionKey: {newDocument.GetPartitionKey()}",
                HttpStatusCode.NotFound,
                0,
                string.Empty,
                0);
        }

        return newDocument;
    }

    public virtual async Task DeleteAsync(
        string documentId,
        string partitionKey,
        ItemRequestOptions? options,
        string? storeName = null,
        CancellationToken cancellationToken = default)
    {
        var success = await storeProvider
            .GetStore(storeName)
            .GetContainer<T>()
            .GetPartition(partitionKey)
            .DeleteDocument(documentId);
        if (!success)
        {
            throw new CosmosException(
                $"Document not found. " +
                $"Id: {documentId}. " +
                $"PartitionKey: {partitionKey}",
                HttpStatusCode.NotFound,
                0,
                string.Empty,
                0);
        }
    }

    public virtual async Task DeletePartitionAsync(
        string partitionKey,
        ItemRequestOptions? options,
        string? storeName = null,
        CancellationToken cancellationToken = default)
        => await storeProvider
            .GetStore(storeName)
            .GetContainer<T>()
            .DeletePartition(partitionKey);

    public virtual async Task<T> UpdateAsync(
        string documentId,
        string partitionKey,
        Func<T, Task<T>> updateDocument,
        int retries = 0,
        string? storeName = null,
        CancellationToken cancellationToken = default)
    {
        var document = storeProvider
            .GetStore(storeName)
            .GetContainer<T>()
            .GetPartition(partitionKey)
            .GetDocument(documentId);
        if (document is not T { } doc)
        {
            throw new CosmosException(
                $"Document not found. " +
                $"Id: {documentId}. " +
                $"PartitionKey: {partitionKey}",
                HttpStatusCode.NotFound,
                0,
                string.Empty,
                0);
        }

        var newDocument = doc.DeepClone(GetSerializerOptions(storeName));
        newDocument = await updateDocument(newDocument);

        await storeProvider
            .GetStore(storeName)
            .GetContainer<T>()
            .GetPartition(partitionKey)
            .ReplaceDocument(
                newDocument.GetDocumentId(),
                newDocument);

        return newDocument;
    }

    public virtual async Task<T> UpdateOrCreateAsync(
        Func<T> getDefaultDocument,
        Func<T, Task<T>> updateDocument,
        int retries = 0,
        string? storeName = null,
        CancellationToken cancellationToken = default)
    {
        var defaultDocument = getDefaultDocument();
        var document = storeProvider
            .GetStore(storeName)
            .GetContainer<T>()
            .GetPartition(defaultDocument.GetPartitionKey())
            .GetDocument(defaultDocument.GetDocumentId()) switch
        {
            T { } doc => doc.DeepClone(GetSerializerOptions(storeName)),
            _ => defaultDocument.DeepClone(GetSerializerOptions(storeName)),
        };

        var newDocument = await updateDocument(document);
        await storeProvider
            .GetStore(storeName)
            .GetContainer<T>()
            .GetPartition(defaultDocument.GetPartitionKey())
            .ReplaceDocument(
                newDocument.GetDocumentId(),
                newDocument);

        return newDocument;
    }

    private JsonSerializerOptions GetSerializerOptions(
        string? storeName)
        => storeProvider
            .GetStore(storeName)
            .SerializerOptions;
}
