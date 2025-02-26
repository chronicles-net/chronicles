using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents.Testing;

public class FakeDocumentTransaction<T> : IDocumentTransaction<T>
    where T : IDocument
{
    private readonly FakeDocumentWriter<T> writer;
    private readonly string partitionKey;
    private readonly List<Func<FakeDocumentWriter<T>, Task<T?>>> operations = [];

    public FakeDocumentTransaction(
        FakeDocumentWriter<T> writer,
        string partitionKey)
    {
        this.writer = writer;
        this.partitionKey = partitionKey;
    }

    public virtual IDocumentTransaction<T> Create<TIn>(
        TIn document,
        TransactionalBatchItemRequestOptions? options = null)
        where TIn : T
    {
        operations.Add(async w => await w.CreateAsync(document, null));
        return this;
    }

    public virtual IDocumentTransaction<T> Delete(
        string id,
        TransactionalBatchItemRequestOptions? options = null)
    {
        operations.Add(async w =>
        {
            await w.DeleteAsync(id, partitionKey, null);
            return default;
        });
        return this;
    }

    public virtual IDocumentTransaction<T> Replace<TIn>(
        TIn document,
        TransactionalBatchItemRequestOptions? options = null)
        where TIn : T
    {
        operations.Add(async w => await w.ReplaceAsync(document, null));
        return this;
    }

    public virtual IDocumentTransaction<T> Write<TIn>(
        TIn document,
        TransactionalBatchItemRequestOptions? options = null)
        where TIn : T
    {
        operations.Add(async w => await w.WriteAsync(document, null));
        return this;
    }

    public virtual Task<TransactionalBatchResponse> CommitAsync(TransactionalBatchRequestOptions options, CancellationToken cancellationToken)
        => CommitAsync(cancellationToken);

    public virtual async Task<TransactionalBatchResponse> CommitAsync(CancellationToken cancellationToken)
    {
        var results = new List<T?>();
        foreach (var operation in operations)
        {
            var result = await operation.Invoke(writer);
            results.Add(result);
        }

        return new FakeTransactionalBatchResponse<T>(results);
    }
}
