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
    /// <returns>The <see cref="QueryDefinition"/> representing the Linq query.</returns>
    public QueryDefinition CreateQuery<TResult>(QueryExpression<T, TResult> query);

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
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <see cref="Task"/> the requested <typeparamref name="T"/> document.</returns>
    public Task<TResult> ReadAsync<TResult>(
        string documentId,
        string partitionKey,
        ItemRequestOptions? options,
        CancellationToken cancellationToken = default)
        where TResult : T;

    /// <summary>
    /// Query documents from the configured Cosmos container and returns a custom result.
    /// </summary>
    /// <typeparam name="TResult">The type used for the custom query result.</typeparam>
    /// <param name="query">Cosmos query to execute.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="options">(Optional) Query request options to use.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> over the requested <typeparamref name="TResult"/> documents.</returns>
    public IAsyncEnumerable<TResult> QueryAsync<TResult>(
        QueryDefinition query,
        string? partitionKey,
        QueryRequestOptions? options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Query documents from the configured Cosmos container using pagination and a custom result.
    /// </summary>
    /// <typeparam name="TResult">The type used for the custom query result.</typeparam>
    /// <param name="query">Cosmos query to execute.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="options">(Optional) Query request options to use.</param>
    /// <param name="maxItemCount">The number of items to return per page.</param>
    /// <param name="continuationToken">(Optional) The continuationToken for getting the next page of a previous query.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <typeparamref name="TResult"/> containing the custom query result.</returns>
    public Task<PagedResult<TResult>> PagedQueryAsync<TResult>(
        QueryDefinition query,
        string? partitionKey,
        QueryRequestOptions? options,
        int? maxItemCount,
        string? continuationToken = default,
        CancellationToken cancellationToken = default);
}
