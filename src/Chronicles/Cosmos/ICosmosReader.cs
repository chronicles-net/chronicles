using System.Net;
using Microsoft.Azure.Cosmos;

namespace Chronicles.Cosmos;

/// <summary>
/// Represents a reader that can read Cosmos documents.
/// </summary>
/// <typeparam name="T">
/// The type of <see cref="ICosmosDocument"/>
/// to be read by this reader.
/// </typeparam>
public interface ICosmosReader<T>
    where T : class
{
    /// <summary>
    /// Creates a <see cref="QueryDefinition"/> from a Linq expression.
    /// </summary>
    /// <typeparam name="TResult">The return type of the </typeparam>
    /// <param name="query"></param>
    /// <returns></returns>
    public QueryDefinition CreateQuery<TResult>(
        Func<IQueryable<T>, IQueryable<TResult>> query);

    /// <summary>
    /// Attempts to read the specified <typeparamref name="T"/> document,
    /// and returns <c>null</c> if none was found.
    /// </summary>
    /// <typeparam name="TResult">
    /// The type used when finding a document.
    /// This can be used when <typeparamref name="T"/> is in it self a generic type.
    /// </typeparam>
    /// <param name="documentId">Id of the document.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="options">(Optional) Query request options to use.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <see cref="Task"/> containing the requested <typeparamref name="T"/> document, or null.</returns>
    Task<TResult?> FindAsync<TResult>(
        string documentId,
        string partitionKey,
        ItemRequestOptions? options,
        CancellationToken cancellationToken = default)
        where TResult : class, T;

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
        where TResult : class, T;

    /// <summary>
    /// Query documents from the configured Cosmos container and returns a custom result.
    /// </summary>
    /// <typeparam name="TResult">The type used for the custom query result.</typeparam>
    /// <param name="query">Cosmos query to execute.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="options">(Optional) Query request options to use.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>An <see cref="IAsyncEnumerable&lt;T&gt;"/> over the requested <typeparamref name="TResult"/> documents.</returns>
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
