using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents.Internal;

public class CosmosTransaction<T> : IDocumentTransaction<T>
    where T : IDocument
{
    private readonly TransactionalBatch transaction;

    public CosmosTransaction(
        TransactionalBatch transaction)
    {
        this.transaction = transaction;
    }

    public IDocumentTransaction<T> Write(
        T document,
        TransactionalBatchItemRequestOptions? options = null)
    {
        transaction.UpsertItem(document, options);
        return this;
    }

    public IDocumentTransaction<T> Create(
        T document,
        TransactionalBatchItemRequestOptions? options = null)
    {
        transaction.CreateItem(document, options);
        return this;
    }

    public IDocumentTransaction<T> Replace(
        T document,
        TransactionalBatchItemRequestOptions? options = null)
    {
        transaction.ReplaceItem(document.GetDocumentId(), document, options);
        return this;
    }

    public IDocumentTransaction<T> Delete(
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
