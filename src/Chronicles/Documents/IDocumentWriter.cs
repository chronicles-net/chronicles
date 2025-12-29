using System.Net;
using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents;

/// <summary>
/// Represents a writer that can write Cosmos documents.
/// </summary>
/// <typeparam name="T">
/// The type of <see cref="IDocument"/>
/// to be written by this writer.
/// </typeparam>
public interface IDocumentWriter<T>
    where T : IDocument
{
    /// <summary>
    /// Creates a new transaction batch used to perform operations across multiple items
    /// in the container with the provided partition key in a transactional manner.
    /// </summary>
    /// <param name="partitionKey">Partition key for the transaction.</param>
    /// <param name="storeName">(Optional) Name of the configured document store.</param>
    /// <returns>A new instance of <see cref="IDocumentTransaction{T}"/>.</returns>
    IDocumentTransaction<T> CreateTransaction(
        string partitionKey,
        string? storeName = null);

    /// <summary>
    /// Creates a new <typeparamref name="T"/> document in Cosmos.
    /// </summary>
    /// <remarks>
    /// When <see cref="ItemRequestOptions.EnableContentResponseOnWrite"/> is set to <c>false</c>,
    /// the document returned is <paramref name="document"/>.
    /// </remarks>
    /// <typeparam name="TIn">The type of document to be created.</typeparam>
    /// <param name="document">The document to be created.</param>
    /// <param name="options">Options for the item request.</param>
    /// <param name="storeName">(Optional) Name of the configured document store.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <see cref="Task"/> containing the created <typeparamref name="T"/> document.</returns>
    Task<TIn> CreateAsync<TIn>(
        TIn document,
        ItemRequestOptions? options,
        string? storeName = null,
        CancellationToken cancellationToken = default)
        where TIn : T;

    /// <summary>
    /// Writes a <typeparamref name="T"/> document to Cosmos, using upsert behavior.
    /// </summary>
    /// <remarks>
    /// When <see cref="ItemRequestOptions.EnableContentResponseOnWrite"/> is set to <c>false</c>,
    /// the document returned is <paramref name="document"/>.
    /// </remarks>
    /// <typeparam name="TIn">The type of document to be written.</typeparam>
    /// <param name="document">The document to be written.</param>
    /// <param name="options">Options for the item request.</param>
    /// <param name="storeName">(Optional) Name of the configured document store.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <see cref="Task"/> containing the written <typeparamref name="T"/> document.</returns>
    Task<TIn> WriteAsync<TIn>(
        TIn document,
        ItemRequestOptions? options,
        string? storeName = null,
        CancellationToken cancellationToken = default)
        where TIn : T;

    /// <summary>
    /// Replaces a <typeparamref name="T"/> document in Cosmos.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A <see cref="CosmosException"/>
    /// with StatusCode <see cref="HttpStatusCode.NotFound"/>
    /// will be thrown if the document does not already exist in Cosmos.
    /// </para>
    /// <para>
    /// A <see cref="CosmosException"/>
    /// with StatusCode <see cref="HttpStatusCode.PreconditionFailed"/>
    /// will be thrown if the document has been updated since it was read
    /// </para>
    /// <para>
    /// When <see cref="ItemRequestOptions.EnableContentResponseOnWrite"/> is set to <c>false</c>,
    /// the document returned is <paramref name="document"/>.
    /// </para>
    /// </remarks>
    /// <typeparam name="TIn">The type of document to be replaced.</typeparam>
    /// <param name="document">The document to be created.</param>
    /// <param name="options">Options for the item request.</param>
    /// <param name="storeName">(Optional) Name of the configured document store.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <see cref="Task"/> containing the updated <typeparamref name="T"/> document.</returns>
    Task<TIn> ReplaceAsync<TIn>(
        TIn document,
        ItemRequestOptions? options,
        string? storeName = null,
        CancellationToken cancellationToken = default)
        where TIn : T;

    /// <summary>
    /// Deletes the specified <typeparamref name="T"/> document from Cosmos.
    /// </summary>
    /// <remarks>
    /// A <see cref="CosmosException"/>
    /// with StatusCode <see cref="HttpStatusCode.NotFound"/>
    /// will be thrown if the document does not already exist in Cosmos.
    /// </remarks>
    /// <param name="documentId">Id of the document.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="options">Options for the item request.</param>
    /// <param name="storeName">(Optional) Name of the configured document store.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task DeleteAsync(
        string documentId,
        string partitionKey,
        ItemRequestOptions? options,
        string? storeName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all documents within a partition from Cosmos.
    /// </summary>
    /// <param name="partitionKey">Partition key of the documents to delete.</param>
    /// <param name="options">Options for the item request.</param>
    /// <param name="storeName">(Optional) Name of the configured document store.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task DeletePartitionAsync(
        string partitionKey,
        ItemRequestOptions? options,
        string? storeName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a <typeparamref name="T"/> document that is read from the configured
    /// Cosmos collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A <see cref="CosmosException"/>
    /// with StatusCode <see cref="HttpStatusCode.NotFound"/>
    /// will be thrown if the document does not already exist in Cosmos.
    /// </para>
    /// <para>
    /// A <see cref="CosmosException"/>
    /// with StatusCode <see cref="HttpStatusCode.PreconditionFailed"/>
    /// will be thrown if the document is being updated simultanious by another thread
    /// and the <paramref name="retries"/> has run out.
    /// </para>
    /// </remarks>
    /// <param name="documentId">Id of the document.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="updateDocument">Function for applying updates to the document.</param>
    /// <param name="retries">Number of retries when a conflict occurs.</param>
    /// <param name="storeName">(Optional) Name of the configured document store.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used.</param>
    /// <returns>A <see cref="Task"/> containing the updated <typeparamref name="T"/> document.</returns>
    Task<T> UpdateAsync(
        string documentId,
        string partitionKey,
        Func<T, Task<T>> updateDocument,
        int retries = 0,
        string? storeName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a <typeparamref name="T"/> document that is read from the configured
    /// Cosmos collection, or creates it if it does not exist.
    /// </summary>
    /// <remarks>
    /// A <see cref="CosmosException"/>
    /// with StatusCode <see cref="HttpStatusCode.PreconditionFailed"/>
    /// will be thrown if the document is being updated simultanious by another thread
    /// and the <paramref name="retries"/> has run out.
    /// </remarks>
    /// <param name="getDefaultDocument">Function for creating the default document. The returned document need to have the DocumentId and PartitionKey set.</param>
    /// <param name="updateDocument">Function for applying updates to the document.</param>
    /// <param name="retries">Number of retries when a conflict occurs.</param>
    /// <param name="storeName">(Optional) Name of the configured document store.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used.</param>
    /// <returns>A <see cref="Task"/> containing the updated <typeparamref name="T"/> document.</returns>
    Task<T> UpdateOrCreateAsync(
        Func<T> getDefaultDocument,
        Func<T, Task<T>> updateDocument,
        int retries = 0,
        string? storeName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to update a document asynchronously if a specified condition is met.
    /// </summary>
    /// <remarks>
    /// If the condition is not met or the document has been changed, the document remains unchanged and the method returns <c>null</c>.
    /// </remarks>
    /// <param name="documentId">The unique identifier of the document to update. Cannot be null or empty.</param>
    /// <param name="partitionKey">The partition key associated with the document. Cannot be null or empty.</param>
    /// <param name="condition">A predicate function that determines whether the update should be applied to the document.</param>
    /// <param name="updateDocument">An asynchronous function that defines how to update the document.</param>
    /// <param name="storeName">(Optional) Name of the configured document store.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.
    /// The task result is the updated document if the condition was met and the update was applied; otherwise, <c>null</c>.</returns>
    Task<T?> ConditionalUpdateAsync(
        string documentId,
        string partitionKey,
        Func<T, bool> condition,
        Func<T, Task<T>> updateDocument,
        string? storeName = null,
        CancellationToken cancellationToken = default);
}
