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

    public IDocumentTransaction<T> Write<TIn>(
        TIn document,
        TransactionalBatchItemRequestOptions? options = null)
        where TIn : T
    {
        transaction.UpsertItem(document, options);
        return this;
    }

    public IDocumentTransaction<T> Create<TIn>(
        TIn document,
        TransactionalBatchItemRequestOptions? options = null)
        where TIn : T
    {
        transaction.CreateItem(document, options);
        return this;
    }

    public IDocumentTransaction<T> Replace<TIn>(
        TIn document,
        TransactionalBatchItemRequestOptions? options = null)
        where TIn : T
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
