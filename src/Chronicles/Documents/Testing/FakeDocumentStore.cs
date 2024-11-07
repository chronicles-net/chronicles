using System.Text.Json;
using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents.Testing;

public sealed class FakeDocumentStore<T> :
    IDocumentReader<T>,
    IDocumentWriter<T>
    where T : IDocument
{
    public FakeDocumentStore()
        : this(
            new FakeDocumentReader<T>(),
            new FakeDocumentWriter<T>())
    {
    }

    public FakeDocumentStore(JsonSerializerOptions options)
        : this(
            new FakeDocumentReader<T>(options),
            new FakeDocumentWriter<T>(options))
    {
    }

    public FakeDocumentStore(
        FakeDocumentReader<T> reader,
        FakeDocumentWriter<T> writer)
    {
        Reader = reader;
        Writer = writer;
        writer.Documents = reader.Documents;
    }

    public IList<T> Documents
    {
        get => Reader.Documents;
        set
        {
            Reader.Documents = value;
            Writer.Documents = value;
        }
    }

    public IList<object> QueryResults
    {
        get => Reader.QueryResults;
        set
        {
            Reader.QueryResults = value;
        }
    }

    public FakeDocumentReader<T> Reader { get; }

    public FakeDocumentWriter<T> Writer { get; }

    QueryDefinition IDocumentReader<T>.CreateQuery<TResult>(
        QueryExpression<T, TResult> query,
        string? storeName)
        => ((IDocumentReader<T>)Reader).CreateQuery(query, storeName);

    Task<TResult> IDocumentReader<T>.ReadAsync<TResult>(
        string documentId,
        string partitionKey,
        ItemRequestOptions? options,
        string? storeName,
        CancellationToken cancellationToken)
        => ((IDocumentReader<T>)Reader)
            .ReadAsync<TResult>(
                documentId,
                partitionKey,
                options,
                storeName,
                cancellationToken);

    IAsyncEnumerable<TResult> IDocumentReader<T>.QueryAsync<TResult>(
        QueryDefinition query,
        string? partitionKey,
        QueryRequestOptions? options,
        string? storeName,
        CancellationToken cancellationToken)
        => ((IDocumentReader<T>)Reader)
            .QueryAsync<TResult>(
                query,
                partitionKey,
                options,
                storeName,
                cancellationToken);

    Task<PagedResult<TResult>> IDocumentReader<T>.PagedQueryAsync<TResult>(
        QueryDefinition query,
        string? partitionKey,
        int? maxItemCount,
        string? continuationToken,
        QueryRequestOptions? options,
        string? storeName,
        CancellationToken cancellationToken)
        => ((IDocumentReader<T>)Reader)
            .PagedQueryAsync<TResult>(
                query,
                partitionKey,
                maxItemCount,
                continuationToken,
                options,
                storeName,
                cancellationToken);

    Task<T> IDocumentWriter<T>.CreateAsync(
        T document,
        ItemRequestOptions? options,
        string? storeName,
        CancellationToken cancellationToken)
        => ((IDocumentWriter<T>)Writer)
            .CreateAsync(
                document,
                options,
                storeName,
                cancellationToken);

    Task<T> IDocumentWriter<T>.WriteAsync(
        T document,
        ItemRequestOptions? options,
        string? storeName,
        CancellationToken cancellationToken)
        => ((IDocumentWriter<T>)Writer)
            .WriteAsync(
                document,
                options,
                storeName,
                cancellationToken);

    Task<T> IDocumentWriter<T>.ReplaceAsync(
        T document,
        ItemRequestOptions? options,
        string? storeName,
        CancellationToken cancellationToken)
        => ((IDocumentWriter<T>)Writer)
            .ReplaceAsync(
                document,
                options,
                storeName,
                cancellationToken);

    Task IDocumentWriter<T>.DeleteAsync(
        string documentId,
        string partitionKey,
        ItemRequestOptions? options,
        string? storeName,
        CancellationToken cancellationToken)
        => ((IDocumentWriter<T>)Writer)
            .DeleteAsync(
                documentId,
                partitionKey,
                options,
                storeName,
                cancellationToken);

    Task<T> IDocumentWriter<T>.UpdateAsync(
        string documentId,
        string partitionKey,
        Func<T, Task> updateDocument,
        int retries,
        string? storeName,
        CancellationToken cancellationToken)
        => ((IDocumentWriter<T>)Writer)
            .UpdateAsync(
                documentId,
                partitionKey,
                updateDocument,
                retries,
                storeName,
                cancellationToken);

    Task<T> IDocumentWriter<T>.UpdateOrCreateAsync(
        Func<T> getDefaultDocument,
        Func<T, Task> updateDocument,
        int retries,
        string? storeName,
        CancellationToken cancellationToken)
        => ((IDocumentWriter<T>)Writer)
            .UpdateOrCreateAsync(
                getDefaultDocument,
                updateDocument,
                retries,
                storeName,
                cancellationToken);

    IDocumentTransaction<T> IDocumentWriter<T>.CreateTransaction(
        string partitionKey,
        string? storeName)
        => ((IDocumentWriter<T>)Writer)
            .CreateTransaction(partitionKey);
}