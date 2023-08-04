using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents;

/// <summary>
/// Represents a batch of operations against items with the same <see cref="PartitionKey"/> in a container that
/// will be performed in a transactional manner at the Azure Cosmos DB service.
/// Use <see cref="ICosmosWriter{T}.CreateTransaction(string)"/> to create an instance of <see cref="ICosmosTransaction{T}" />.
/// </summary>
/// <typeparam name="T">
/// The type of <see cref="ICosmosDocument"/>
/// to be handled by this transaction.
/// </typeparam>
public interface ICosmosTransaction<T>
    where T : ICosmosDocument
{
    /// <summary>
    /// Adds an operation to write a document into the batch.
    /// </summary>
    /// <param name="document">The document to be written.</param>
    /// <param name="options">(Optional) The options for the item request.</param>
    /// <returns>The transaction instance with the operation added.</returns>
    ICosmosTransaction<T> Write(
        T document,
        TransactionalBatchItemRequestOptions? options = null);

    /// <summary>
    /// Adds an operation to create a new document into the batch.
    /// </summary>
    /// <param name="document">The document to be created.</param>
    /// <param name="options">(Optional) The options for the item request.</param>
    /// <returns>The transaction instance with the operation added.</returns>
    ICosmosTransaction<T> Create(
        T document,
        TransactionalBatchItemRequestOptions? options = null);

    /// <summary>
    /// Adds an operation to replace a document item into the batch.
    /// </summary>
    /// <param name="document">The document to be created.</param>
    /// <param name="options">(Optional) The options for the item request.</param>
    /// <returns>The transaction instance with the operation added.</returns>
    ICosmosTransaction<T> Replace(
        T document,
        TransactionalBatchItemRequestOptions? options = null);

    /// <summary>
    /// Adds an operation to delete a document into the batch.
    /// </summary>
    /// <param name="id">The unique id of the document.</param>
    /// <param name="options">(Optional) The options for the item request.</param>
    /// <returns>The transaction instance with the operation added.</returns>
    ICosmosTransaction<T> Delete(
        string id,
        TransactionalBatchItemRequestOptions? options = null);

    /// <summary>
    /// Executes the transaction at the Azure Cosmos service as an asynchronous operation.
    /// </summary>
    /// <param name="cancellationToken">(Optional) Cancellation token representing request cancellation.</param>
    /// <returns>An awaitable response which contains details of execution of the transactional batch.</returns>
    Task<TransactionalBatchResponse> CommitAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Executes the transaction at the Azure Cosmos service as an asynchronous operation.
    /// </summary>
    /// <param name="options">Options that apply specifically to batch request.</param>
    /// <param name="cancellationToken">(Optional) Cancellation token representing request cancellation.</param>
    /// <returns>An awaitable response which contains details of execution of the transactional batch.</returns>
    Task<TransactionalBatchResponse> CommitAsync(
        TransactionalBatchRequestOptions options,
        CancellationToken cancellationToken);
}
