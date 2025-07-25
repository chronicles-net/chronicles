using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents.Testing;

public class FakeDocumentTransaction<T> : IDocumentTransaction<T>
    where T : IDocument
{
    private readonly FakePartition partition;
    private readonly FakePartitionTransaction transaction;

    public FakeDocumentTransaction(
        FakePartition partition)
    {
        this.partition = partition;
        transaction = partition.CreateTransaction();
    }

    public virtual IDocumentTransaction<T> Create<TIn>(
        TIn document,
        TransactionalBatchItemRequestOptions? options = null)
        where TIn : T
    {
        transaction.CreateDocument(document.GetDocumentId(), document);

        return this;
    }

    public virtual IDocumentTransaction<T> Delete(
        string id,
        TransactionalBatchItemRequestOptions? options = null)
    {
        transaction.DeleteDocument(id);

        return this;
    }

    public virtual IDocumentTransaction<T> Replace<TIn>(
        TIn document,
        TransactionalBatchItemRequestOptions? options = null)
        where TIn : T
    {
        transaction.ReplaceDocument(document.GetDocumentId(), document);

        return this;
    }

    public virtual IDocumentTransaction<T> Write<TIn>(
        TIn document,
        TransactionalBatchItemRequestOptions? options = null)
        where TIn : T
    {
        transaction.UpsertDocument(document.GetDocumentId(), document);

        return this;
    }

    public virtual Task<TransactionalBatchResponse> CommitAsync(
        TransactionalBatchRequestOptions options,
        CancellationToken cancellationToken)
        => CommitAsync(cancellationToken);

    public virtual async Task<TransactionalBatchResponse> CommitAsync(
        CancellationToken cancellationToken)
    {
        var result = transaction.Commit();

        await partition.CommitAsync(result);

        return result;
    }
}
