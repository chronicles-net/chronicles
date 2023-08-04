using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents.Testing;

public class FakeCosmosTransaction<T> : ICosmosTransaction<T>
    where T : class, ICosmosDocument
{
    private readonly FakeCosmosWriter<T> writer;
    private readonly string partitionKey;
    private readonly List<Func<FakeCosmosWriter<T>, Task<T?>>> operations = new();

    public FakeCosmosTransaction(
        FakeCosmosWriter<T> writer,
        string partitionKey)
    {
        this.writer = writer;
        this.partitionKey = partitionKey;
    }

    public virtual ICosmosTransaction<T> Create(
        T document,
        TransactionalBatchItemRequestOptions? options = null)
    {
        operations.Add(async w => await w.CreateAsync(document, null));
        return this;
    }

    public virtual ICosmosTransaction<T> Delete(
        string id,
        TransactionalBatchItemRequestOptions? options = null)
    {
        operations.Add(async w =>
        {
            await w.DeleteAsync(id, partitionKey, null);
            return null;
        });
        return this;
    }

    public virtual ICosmosTransaction<T> Replace(
        T document,
        TransactionalBatchItemRequestOptions? options = null)
    {
        operations.Add(async w => await w.ReplaceAsync(document, null));
        return this;
    }

    public virtual ICosmosTransaction<T> Write(
        T document,
        TransactionalBatchItemRequestOptions? options = null)
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
