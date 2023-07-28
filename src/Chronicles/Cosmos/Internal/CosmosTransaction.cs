using Microsoft.Azure.Cosmos;

namespace Chronicles.Cosmos.Internal;

public class CosmosTransaction<T> : ICosmosTransaction<T>
    where T : ICosmosDocument
{
    private readonly TransactionalBatch transaction;

    public CosmosTransaction(
        TransactionalBatch transaction)
    {
        this.transaction = transaction;
    }

    public ICosmosTransaction<T> Write(
        T doc,
        TransactionalBatchItemRequestOptions? options = null)
    {
        transaction.UpsertItem(doc, options);
        return this;
    }

    public ICosmosTransaction<T> Create(
        T doc,
        TransactionalBatchItemRequestOptions? options = null)
    {
        transaction.CreateItem(doc, options);
        return this;
    }

    public ICosmosTransaction<T> Replace(
        T doc,
        TransactionalBatchItemRequestOptions? options = null)
    {
        transaction.ReplaceItem(doc.DocumentId, doc, options);
        return this;
    }

    public ICosmosTransaction<T> Delete(
        string id,
        TransactionalBatchItemRequestOptions? options = null)
    {
        transaction.DeleteItem(id, options);
        return this;
    }

    public Task CommitAsync(CancellationToken cancellationToken)
        => transaction.ExecuteAsync(cancellationToken);

    public Task CommitAsync(
        TransactionalBatchRequestOptions options,
        CancellationToken cancellationToken)
        => transaction.ExecuteAsync(options, cancellationToken);
}
