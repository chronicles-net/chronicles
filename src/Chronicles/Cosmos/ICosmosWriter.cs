using System.Net;
using Microsoft.Azure.Cosmos;

namespace Chronicles.Cosmos;

/// <summary>
/// Represents a writer that can write Cosmos documents.
/// </summary>
/// <typeparam name="T">
/// The type of <see cref="ICosmosDocument"/>
/// to be written by this writer.
/// </typeparam>
public interface ICosmosWriter<T>
    where T : ICosmosDocument
{
    /// <summary>
    /// Creates a new transaction batch used to perform operations across multiple items
    /// in the container with the provided partition key in a transactional manner.
    /// </summary>
    /// <param name="partitionKey">Partition key for the transaction.</param>
    /// <returns>A new instance of <see cref="ICosmosTransaction{T}"/>.</returns>
    ICosmosTransaction<T> CreateTransaction(
        string partitionKey);

    /// <summary>
    /// Creates a new <typeparamref name="T"/> document in Cosmos.
    /// </summary>
    /// <remarks>
    /// A <see cref="CosmosException"/>
    /// with StatusCode <see cref="HttpStatusCode.Conflict"/>
    /// will be thrown if a document already exists.
    /// </remarks>
    /// <param name="document">The document to be created.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <see cref="Task"/> containing the created <typeparamref name="T"/> document.</returns>
    Task<T> CreateAsync(
        T document,
        CancellationToken cancellationToken = default)
        => CreateAsync(document, null, cancellationToken);

    /// <summary>
    /// Creates a new <typeparamref name="T"/> document in Cosmos.
    /// </summary>
    /// <remarks>
    /// When <see cref="ItemRequestOptions.EnableContentResponseOnWrite"/> is set to <c>false</c>,
    /// the document returned is <paramref name="document"/>.
    /// </remarks>
    /// <param name="document">The document to be created.</param>
    /// <param name="options">Options for the item request.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <see cref="Task"/> containing the created <typeparamref name="T"/> document.</returns>
    Task<T> CreateAsync(
        T document,
        ItemRequestOptions? options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a <typeparamref name="T"/> document to Cosmos, using upsert behavior.
    /// </summary>
    /// <param name="document">The document to be written.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <see cref="Task"/> containing the written <typeparamref name="T"/> document.</returns>
    Task<T> WriteAsync(
        T document,
        CancellationToken cancellationToken = default)
        => WriteAsync(document, null, cancellationToken);

    /// <summary>
    /// Writes a <typeparamref name="T"/> document to Cosmos, using upsert behavior.
    /// </summary>
    /// <remarks>
    /// When <see cref="ItemRequestOptions.EnableContentResponseOnWrite"/> is set to <c>false</c>,
    /// the document returned is <paramref name="document"/>.
    /// </remarks>
    /// <param name="document">The document to be written.</param>
    /// <param name="options">Options for the item request.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <see cref="Task"/> containing the written <typeparamref name="T"/> document.</returns>
    Task<T> WriteAsync(
        T document,
        ItemRequestOptions? options,
        CancellationToken cancellationToken = default);

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
    /// (using the <see cref="ICosmosDocument.ETag"/> to match the version).
    /// </para>
    /// </remarks>
    /// <param name="document">The document to be created.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <see cref="Task"/> containing the updated <typeparamref name="T"/> document.</returns>
    Task<T> ReplaceAsync(
        T document,
        CancellationToken cancellationToken = default)
        => ReplaceAsync(document, null, cancellationToken);

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
    /// (using the <see cref="ICosmosDocument.ETag"/> to match the version).
    /// </para>
    /// <para>
    /// When <see cref="ItemRequestOptions.EnableContentResponseOnWrite"/> is set to <c>false</c>,
    /// the document returned is <paramref name="document"/>.
    /// </para>
    /// </remarks>
    /// <param name="document">The document to be created.</param>
    /// <param name="options">Options for the item request.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <see cref="Task"/> containing the updated <typeparamref name="T"/> document.</returns>
    Task<T> ReplaceAsync(
        T document,
        ItemRequestOptions? options,
        CancellationToken cancellationToken = default);

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
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task DeleteAsync(
        string documentId,
        string partitionKey,
        CancellationToken cancellationToken = default)
        => DeleteAsync(documentId, partitionKey, null, cancellationToken);

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
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task DeleteAsync(
        string documentId,
        string partitionKey,
        ItemRequestOptions? options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tries to delete the specified <typeparamref name="T"/> document from Cosmos.
    /// </summary>
    /// <remarks>
    /// When trying to delete a non existing document, False is returned.
    /// </remarks>
    /// <param name="documentId">Id of the document.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns><c>True</c> if document was deleted otherwise <c>False</c>.</returns>
    public Task<bool> TryDeleteAsync(
        string documentId,
        string partitionKey,
        CancellationToken cancellationToken = default)
        => TryDeleteAsync(documentId, partitionKey, null, cancellationToken);

    /// <summary>
    /// Tries to delete the specified <typeparamref name="T"/> document from Cosmos.
    /// </summary>
    /// <remarks>
    /// When trying to delete a non existing document, False is returned.
    /// </remarks>
    /// <param name="documentId">Id of the document.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="options">Options for the item request.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns><c>True</c> if document was deleted otherwise <c>False</c>.</returns>
    public Task<bool> TryDeleteAsync(
        string documentId,
        string partitionKey,
        ItemRequestOptions? options,
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
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used.</param>
    /// <returns>A <see cref="Task"/> containing the updated <typeparamref name="T"/> document.</returns>
    Task<T> UpdateAsync(
        string documentId,
        string partitionKey,
        Func<T, Task> updateDocument,
        int retries = 0,
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
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used.</param>
    /// <returns>A <see cref="Task"/> containing the updated <typeparamref name="T"/> document.</returns>
    Task<T> UpdateAsync(
        string documentId,
        string partitionKey,
        Action<T> updateDocument,
        int retries = 0,
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
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used.</param>
    /// <returns>A <see cref="Task"/> containing the updated <typeparamref name="T"/> document.</returns>
    Task<T> UpdateOrCreateAsync(
        Func<T> getDefaultDocument,
        Func<T, Task> updateDocument,
        int retries = 0,
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
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used.</param>
    /// <returns>A <see cref="Task"/> containing the updated <typeparamref name="T"/> document.</returns>
    Task<T> UpdateOrCreateAsync(
        Func<T> getDefaultDocument,
        Action<T> updateDocument,
        int retries = 0,
        CancellationToken cancellationToken = default);
}