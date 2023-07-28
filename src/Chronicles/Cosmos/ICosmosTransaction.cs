using Microsoft.Azure.Cosmos;

namespace Chronicles.Cosmos;

public interface ICosmosTransaction<T>
    where T : ICosmosDocument
{
    ICosmosTransaction<T> Write(
        T doc,
        TransactionalBatchItemRequestOptions? options = null);

    ICosmosTransaction<T> Create(
        T doc,
        TransactionalBatchItemRequestOptions? options = null);

    ICosmosTransaction<T> Replace(
        T doc,
        TransactionalBatchItemRequestOptions? options = null);

    ICosmosTransaction<T> Delete(
        string id,
        TransactionalBatchItemRequestOptions? options = null);

    Task CommitAsync(
        CancellationToken cancellationToken);

    Task CommitAsync(
        TransactionalBatchRequestOptions options,
        CancellationToken cancellationToken);
}
