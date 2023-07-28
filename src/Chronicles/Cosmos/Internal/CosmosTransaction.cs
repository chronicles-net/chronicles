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
        T document,
        TransactionalBatchItemRequestOptions? options = null)
    {
        transaction.UpsertItem(document, options);
        return this;
    }

    public ICosmosTransaction<T> Create(
        T document,
        TransactionalBatchItemRequestOptions? options = null)
    {
        transaction.CreateItem(document, options);
        return this;
    }

    public ICosmosTransaction<T> Replace(
        T document,
        TransactionalBatchItemRequestOptions? options = null) 
    {
        transaction.ReplaceItem(document.DocumentId, document, options);
        return this;
    }

    public ICosmosTransaction<T> Delete(
        string id,
        TransactionalBatchItemRequestOptions? options = null)
    {
        transaction.DeleteItem(id, options);
        return this;
    }

    public Task<TransactionalBatchResponse> CommitAsync(CancellationToken cancellationToken)
        => transaction.ExecuteAsync(cancellationToken);

    public Task<TransactionalBatchResponse> CommitAsync(
        TransactionalBatchRequestOptions options,
        CancellationToken cancellationToken)
        => transaction.ExecuteAsync(options, cancellationToken);
}
