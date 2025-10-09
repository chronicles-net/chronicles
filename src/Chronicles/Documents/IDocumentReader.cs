using System.Net;
using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents;

/// <summary>
/// Represents a reader that can read Cosmos documents.
/// </summary>
/// <typeparam name="T">
/// The type of <see cref="IDocument"/>
/// to be read by this reader.
/// </typeparam>
public interface IDocumentReader<T>
{
    /// <summary>
    /// Creates a <see cref="QueryDefinition"/> from a Linq expression.
    /// </summary>
    /// <typeparam name="TResult">The return type of the </typeparam>
    /// <param name="query">The Linq query to use for the <see cref="QueryDefinition"/>.</param>
    /// <param name="storeName">(Optional) Name of the configured document store.</param>
    /// <returns>The <see cref="QueryDefinition"/> representing the Linq query.</returns>
    public QueryDefinition CreateQuery<TResult>(
        QueryExpression<T, TResult> query,
        string? storeName = null);

    /// <summary>
    /// Reads the specified <typeparamref name="T"/> document from the configured
    /// Cosmos collection.
    /// </summary>
    /// <remarks>
    /// A <see cref="CosmosException"/>
    /// with StatusCode <see cref="HttpStatusCode.NotFound"/>
    /// will be thrown if document could not be found.
    /// </remarks>
    /// <typeparam name="TResult">
    /// The type used when finding a document.
    /// This can be used when <typeparamref name="T"/> is in it self a generic type.
    /// </typeparam>
    /// <param name="documentId">Id of the document.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="options">(Optional) Query request options to use.</param>
    /// <param name="storeName">(Optional) Name of the configured document store.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <see cref="Task"/> the requested <typeparamref name="T"/> document.</returns>
    public Task<TResult> ReadAsync<TResult>(
        string documentId,
        string partitionKey,
        ItemRequestOptions? options,
        string? storeName = null,
        CancellationToken cancellationToken = default)
        where TResult : T;

    /// <summary>
    /// Reads multiple documents by ID and partition key from the store asynchronously and
    /// returns the results as a collection of the specified type.
    /// </summary>
    /// <typeparam name="TResult">
    /// The type into which each retrieved document is deserialized.
    /// This can be used when <typeparamref name="T"/> is in it self a generic type.
    /// </typeparam>
    /// <param name="ids">
    /// An array of tuples, each containing the document ID and partition key for a document to be read.
    /// Cannot be null or contain null document IDs.</param>
    /// <param name="options">
    /// Optional request options that control aspects of the read operation, such as consistency
    /// level or session token. May be null.
    /// </param>
    /// <param name="storeName">The name of the store to read from. If null, the default store is used.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains an enumerable
    /// collection of deserialized documents of type <typeparamref name="TResult"/>.
    /// The collection may be empty if no documents are found.
    /// </returns>
    public Task<IEnumerable<TResult>> ReadManyAsync<TResult>(
        (string documentId, string partitionKey)[] ids,
        ReadManyRequestOptions? options,
        string? storeName = null,
        CancellationToken cancellationToken = default)
        where TResult : T;

    /// <summary>
    /// Query documents from the configured Cosmos container and returns a custom result.
    /// </summary>
    /// <typeparam name="TResult">The type used for the custom query result.</typeparam>
    /// <param name="query">Cosmos query to execute.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="options">(Optional) Query request options to use.</param>
    /// <param name="storeName">(Optional) Name of the configured document store.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> over the requested <typeparamref name="TResult"/> documents.</returns>
    public IAsyncEnumerable<TResult> QueryAsync<TResult>(
        QueryDefinition query,
        string? partitionKey,
        QueryRequestOptions? options,
        string? storeName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Query documents from the configured Cosmos container using pagination and a custom result.
    /// </summary>
    /// <typeparam name="TResult">The type used for the custom query result.</typeparam>
    /// <param name="query">Cosmos query to execute.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="maxItemCount">The number of items to return per page.</param>
    /// <param name="continuationToken">(Optional) The continuationToken for getting the next page of a previous query.</param>
    /// <param name="options">(Optional) Query request options to use.</param>
    /// <param name="storeName">(Optional) Name of the configured document store.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <typeparamref name="TResult"/> containing the custom query result.</returns>
    public Task<PagedResult<TResult>> PagedQueryAsync<TResult>(
        QueryDefinition query,
        string? partitionKey,
        int? maxItemCount,
        string? continuationToken,
        QueryRequestOptions? options,
        string? storeName = null,
        CancellationToken cancellationToken = default);
}
