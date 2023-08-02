using System.Net;
using Microsoft.Azure.Cosmos;

namespace Chronicles.Cosmos;

public static class CosmosWriterExtensions
{
    /// <summary>
    /// Creates a new <typeparamref name="T"/> document in Cosmos.
    /// </summary>
    /// <remarks>
    /// A <see cref="CosmosException"/>
    /// with StatusCode <see cref="HttpStatusCode.Conflict"/>
    /// will be thrown if a document already exists.
    /// </remarks>
    /// <typeparam name="T">The type used by the <see cref="ICosmosReader{T}"/>.</typeparam>
    /// <param name="writer">The <see cref="ICosmosWriter{T}"/>.</param>
    /// <param name="document">The document to be created.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <see cref="Task"/> containing the created <typeparamref name="T"/> document.</returns>
    public static Task<T> CreateAsync<T>(
        this ICosmosWriter<T> writer,
        T document,
        CancellationToken cancellationToken = default)
        where T : ICosmosDocument
        => writer.CreateAsync(
            document,
            options: null,
            cancellationToken);

    /// <summary>
    /// Writes a <typeparamref name="T"/> document to Cosmos, using upsert behavior.
    /// </summary>
    /// <typeparam name="T">The type used by the <see cref="ICosmosReader{T}"/>.</typeparam>
    /// <param name="writer">The <see cref="ICosmosWriter{T}"/>.</param>
    /// <param name="document">The document to be written.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <see cref="Task"/> containing the written <typeparamref name="T"/> document.</returns>
    public static Task<T> WriteAsync<T>(
        this ICosmosWriter<T> writer,
        T document,
        CancellationToken cancellationToken = default)
        where T : ICosmosDocument
        => writer.WriteAsync(
            document,
            options: null,
            cancellationToken);

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
    /// will be thrown if the document has been updated since it was read.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The type used by the <see cref="ICosmosReader{T}"/>.</typeparam>
    /// <param name="writer">The <see cref="ICosmosWriter{T}"/>.</param>
    /// <param name="document">The document to be created.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <see cref="Task"/> containing the updated <typeparamref name="T"/> document.</returns>
    public static Task<T> ReplaceAsync<T>(
        this ICosmosWriter<T> writer,
        T document,
        CancellationToken cancellationToken = default)
        where T : ICosmosDocument
        => writer.ReplaceAsync(
            document,
            options: null,
            cancellationToken);


    /// <summary>
    /// Deletes the specified <typeparamref name="T"/> document from Cosmos.
    /// </summary>
    /// <remarks>
    /// A <see cref="CosmosException"/>
    /// with StatusCode <see cref="HttpStatusCode.NotFound"/>
    /// will be thrown if the document does not already exist in Cosmos.
    /// </remarks>
    /// <typeparam name="T">The type used by the <see cref="ICosmosReader{T}"/>.</typeparam>
    /// <param name="writer">The <see cref="ICosmosWriter{T}"/>.</param>
    /// <param name="documentId">Id of the document.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static Task DeleteAsync<T>(
        this ICosmosWriter<T> writer,
        string documentId,
        string partitionKey,
        CancellationToken cancellationToken = default)
        where T : ICosmosDocument
        => writer.DeleteAsync(
            documentId,
            partitionKey,
            options: null,
            cancellationToken);

    /// <summary>
    /// Tries to delete the specified <typeparamref name="T"/> document from Cosmos.
    /// </summary>
    /// <remarks>
    /// When trying to delete a non existing document, False is returned.
    /// </remarks>
    /// <typeparam name="T">The type used by the <see cref="ICosmosReader{T}"/>.</typeparam>
    /// <param name="writer">The <see cref="ICosmosWriter{T}"/>.</param>
    /// <param name="documentId">Id of the document.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns><c>True</c> if document was deleted otherwise <c>False</c>.</returns>
    public static Task<bool> TryDeleteAsync<T>(
        this ICosmosWriter<T> writer,
        string documentId,
        string partitionKey,
        CancellationToken cancellationToken = default)
        where T : ICosmosDocument
        => writer.TryDeleteAsync(
            documentId,
            partitionKey,
            options: null,
            cancellationToken);

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
    /// <typeparam name="T">The type used by the <see cref="ICosmosReader{T}"/>.</typeparam>
    /// <param name="writer">The <see cref="ICosmosWriter{T}"/>.</param>
    /// <param name="documentId">Id of the document.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="updateDocument">Function for applying updates to the document.</param>
    /// <param name="retries">Number of retries when a conflict occurs.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used.</param>
    /// <returns>A <see cref="Task"/> containing the updated <typeparamref name="T"/> document.</returns>
    public static Task<T> UpdateAsync<T>(
        this ICosmosWriter<T> writer,
        string documentId,
        string partitionKey,
        Action<T> updateDocument,
        int retries = 0,
        CancellationToken cancellationToken = default)
        where T : ICosmosDocument
        => writer.UpdateAsync(
            documentId,
            partitionKey,
            d =>
            {
                updateDocument(d);
                return Task.CompletedTask;
            },
            retries,
            cancellationToken);

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
    /// <typeparam name="T">The type used by the <see cref="ICosmosReader{T}"/>.</typeparam>
    /// <param name="writer">The <see cref="ICosmosWriter{T}"/>.</param>
    /// <param name="getDefaultDocument">Function for creating the default document. The returned document need to have the DocumentId and PartitionKey set.</param>
    /// <param name="updateDocument">Function for applying updates to the document.</param>
    /// <param name="retries">Number of retries when a conflict occurs.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used.</param>
    /// <returns>A <see cref="Task"/> containing the updated <typeparamref name="T"/> document.</returns>
    public static Task<T> UpdateOrCreateAsync<T>(
        this ICosmosWriter<T> writer,
        Func<T> getDefaultDocument,
        Action<T> updateDocument,
        int retries = 0,
        CancellationToken cancellationToken = default)
        where T : ICosmosDocument
        => writer.UpdateOrCreateAsync(
            getDefaultDocument,
            d =>
            {
                updateDocument(d);
                return Task.CompletedTask;
            },
            retries,
            cancellationToken);
}
