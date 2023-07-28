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
    /// Delegate for building a cosmos linq query.
    /// </summary>
    /// <typeparam name="TResult">The type of document to be read from cosmos.</typeparam>
    /// <param name="query">Document linq query.</param>
    /// <returns>Linq query expression to execute.</returns>
    public delegate IQueryable<TResult> QueryExpression<TResult>(IQueryable<T> query);

    /// <summary>
    /// Attempts to read the specified <typeparamref name="T"/> document,
    /// and returns <c>null</c> if none was found.
    /// </summary>
    /// <param name="documentId">Id of the document.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <see cref="Task"/> containing the requested <typeparamref name="T"/> document, or null.</returns>
    Task<T?> FindAsync(
        string documentId,
        string partitionKey,
        CancellationToken cancellationToken = default)
        => FindAsync(documentId, partitionKey, null, cancellationToken);

    /// <summary>
    /// Attempts to read the specified <typeparamref name="T"/> document,
    /// and returns <c>null</c> if none was found.
    /// </summary>
    /// <param name="documentId">Id of the document.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="options">(Optional) Query request options to use.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <see cref="Task"/> containing the requested <typeparamref name="T"/> document, or null.</returns>
    Task<T?> FindAsync(
        string documentId,
        string partitionKey,
        ItemRequestOptions? options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the specified <typeparamref name="T"/> document from the configured
    /// Cosmos collection.
    /// </summary>
    /// <remarks>
    /// A <see cref="CosmosException"/>
    /// with StatusCode <see cref="HttpStatusCode.NotFound"/>
    /// will be thrown if document could not be found.
    /// </remarks>
    /// <param name="documentId">Id of the document.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <see cref="Task"/> the requested <typeparamref name="T"/> document.</returns>
    public Task<T> ReadAsync(
        string documentId,
        string partitionKey,
        CancellationToken cancellationToken = default)
        => ReadAsync(documentId, partitionKey, null, cancellationToken);

    /// <summary>
    /// Reads the specified <typeparamref name="T"/> document from the configured
    /// Cosmos collection.
    /// </summary>
    /// <remarks>
    /// A <see cref="CosmosException"/>
    /// with StatusCode <see cref="HttpStatusCode.NotFound"/>
    /// will be thrown if document could not be found.
    /// </remarks>
    /// <param name="documentId">Id of the document.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="options">(Optional) Query request options to use.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <see cref="Task"/> the requested <typeparamref name="T"/> document.</returns>
    public Task<T> ReadAsync(
        string documentId,
        string partitionKey,
        ItemRequestOptions? options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads all the specified <typeparamref name="T"/> document from the configured
    /// Cosmos collection.
    /// </summary>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>An <see cref="IAsyncEnumerable&lt;T&gt;"/> over all the <typeparamref name="T"/> documents.</returns>
    public IAsyncEnumerable<T> ReadAllAsync(
        string? partitionKey,
        CancellationToken cancellationToken = default)
        => ReadAllAsync(partitionKey, null, cancellationToken);

    /// <summary>
    /// Reads all the specified <typeparamref name="T"/> document from the configured
    /// Cosmos collection.
    /// </summary>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="options">(Optional) Query request options to use.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>An <see cref="IAsyncEnumerable&lt;T&gt;"/> over all the <typeparamref name="T"/> documents.</returns>
    public IAsyncEnumerable<T> ReadAllAsync(
        string? partitionKey,
        QueryRequestOptions? options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Query documents from the configured Cosmos container.
    /// </summary>
    /// <param name="query">Cosmos query to execute.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>An <see cref="IAsyncEnumerable&lt;T&gt;"/> over the requested <typeparamref name="T"/> documents.</returns>
    public IAsyncEnumerable<T> QueryAsync(
        QueryDefinition query,
        string? partitionKey,
        CancellationToken cancellationToken = default)
        => QueryAsync<T>(query, null, partitionKey, cancellationToken);

    /// <summary>
    /// Query documents from the configured Cosmos container and returns a custom result.
    /// </summary>
    /// <typeparam name="TResult">The type used for the custom query result.</typeparam>
    /// <param name="query">Cosmos query to execute.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>An <see cref="IAsyncEnumerable&lt;T&gt;"/> over the requested <typeparamref name="TResult"/> documents.</returns>
    public IAsyncEnumerable<TResult> QueryAsync<TResult>(
        QueryDefinition query,
        string? partitionKey,
        CancellationToken cancellationToken = default)
        => QueryAsync<TResult>(query, null, partitionKey, cancellationToken);

    /// <summary>
    /// Query documents from the configured Cosmos container and returns a custom result.
    /// </summary>
    /// <typeparam name="TResult">The type used for the custom query result.</typeparam>
    /// <param name="query">Cosmos query to execute.</param>
    /// <param name="options">(Optional) Query request options to use.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>An <see cref="IAsyncEnumerable&lt;T&gt;"/> over the requested <typeparamref name="TResult"/> documents.</returns>
    public IAsyncEnumerable<TResult> QueryAsync<TResult>(
        QueryDefinition query,
        QueryRequestOptions? options,
        string? partitionKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Query documents from the configured Cosmos container.
    /// </summary>
    /// <param name="query">Cosmos Linq query to execute.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>An <see cref="IAsyncEnumerable&lt;T&gt;"/> over the requested <typeparamref name="T"/> documents.</returns>
    public IAsyncEnumerable<T> QueryAsync(
        QueryExpression<T> query,
        string? partitionKey,
        CancellationToken cancellationToken = default)
        => QueryAsync<T>(query, null, partitionKey, cancellationToken);

    /// <summary>
    /// Query documents from the configured Cosmos container and returns a custom result.
    /// </summary>
    /// <typeparam name="TResult">The type used for the custom query result.</typeparam>
    /// <param name="query">Cosmos Linq query to execute.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>An <see cref="IAsyncEnumerable&lt;T&gt;"/> over the requested <typeparamref name="TResult"/> documents.</returns>
    public IAsyncEnumerable<TResult> QueryAsync<TResult>(
        QueryExpression<TResult> query,
        string? partitionKey,
        CancellationToken cancellationToken = default)
        => QueryAsync<TResult>(query, null, partitionKey, cancellationToken);

    /// <summary>
    /// Query documents from the configured Cosmos container and returns a custom result.
    /// </summary>
    /// <typeparam name="TResult">The type used for the custom query result.</typeparam>
    /// <param name="query">Cosmos Linq query to execute.</param>
    /// <param name="options">(Optional) Query request options to use.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>An <see cref="IAsyncEnumerable&lt;T&gt;"/> over the requested <typeparamref name="TResult"/> documents.</returns>
    public IAsyncEnumerable<TResult> QueryAsync<TResult>(
        QueryExpression<TResult> query,
        QueryRequestOptions? options,
        string? partitionKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Query documents from the configured Cosmos container using pagination.
    /// </summary>
    /// <param name="query">Cosmos query to execute.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="maxItemCount">The number of items to return per page.</param>
    /// <param name="continuationToken">(Optional) The continuationToken for getting the next page of a previous query.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>An <see cref="IAsyncEnumerable&lt;T&gt;"/> over the requested <typeparamref name="T"/> documents.</returns>
    public Task<PagedResult<T>> PagedQueryAsync(
        QueryDefinition query,
        string? partitionKey,
        int? maxItemCount,
        string? continuationToken = default,
        CancellationToken cancellationToken = default)
        => PagedQueryAsync<T>(query, null, partitionKey, maxItemCount, continuationToken, cancellationToken);

    /// <summary>
    /// Query documents from the configured Cosmos container using pagination and a custom result.
    /// </summary>
    /// <typeparam name="TResult">The type used for the custom query result.</typeparam>
    /// <param name="query">Cosmos query to execute.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="maxItemCount">The number of items to return per page.</param>
    /// <param name="continuationToken">(Optional) The continuationToken for getting the next page of a previous query.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <typeparamref name="TResult"/> containing the custom query result.</returns>
    public Task<PagedResult<TResult>> PagedQueryAsync<TResult>(
        QueryDefinition query,
        string? partitionKey,
        int? maxItemCount,
        string? continuationToken = default,
        CancellationToken cancellationToken = default)
        => PagedQueryAsync<TResult>(query, null, partitionKey, maxItemCount, continuationToken, cancellationToken);

    /// <summary>
    /// Query documents from the configured Cosmos container using pagination and a custom result.
    /// </summary>
    /// <typeparam name="TResult">The type used for the custom query result.</typeparam>
    /// <param name="query">Cosmos query to execute.</param>
    /// <param name="options">(Optional) Query request options to use.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="maxItemCount">The number of items to return per page.</param>
    /// <param name="continuationToken">(Optional) The continuationToken for getting the next page of a previous query.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <typeparamref name="TResult"/> containing the custom query result.</returns>
    public Task<PagedResult<TResult>> PagedQueryAsync<TResult>(
        QueryDefinition query,
        QueryRequestOptions? options,
        string? partitionKey,
        int? maxItemCount,
        string? continuationToken = default,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Query documents from the configured Cosmos container using pagination.
    /// </summary>
    /// <param name="query">Cosmos linq query to execute.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="maxItemCount">The number of items to return per page.</param>
    /// <param name="continuationToken">(Optional) The continuationToken for getting the next page of a previous query.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>An <see cref="IAsyncEnumerable&lt;T&gt;"/> over the requested <typeparamref name="T"/> documents.</returns>
    public Task<PagedResult<T>> PagedQueryAsync(
        QueryExpression<T> query,
        string? partitionKey,
        int? maxItemCount,
        string? continuationToken = default,
        CancellationToken cancellationToken = default)
        => PagedQueryAsync<T>(query, null, partitionKey, maxItemCount, continuationToken, cancellationToken);

    /// <summary>
    /// Query documents from the configured Cosmos container using pagination and a custom result.
    /// </summary>
    /// <typeparam name="TResult">The type used for the custom query result.</typeparam>
    /// <param name="query">Cosmos linq query to execute.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="maxItemCount">The number of items to return per page.</param>
    /// <param name="continuationToken">(Optional) The continuationToken for getting the next page of a previous query.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <typeparamref name="TResult"/> containing the custom query result.</returns>
    public Task<PagedResult<TResult>> PagedQueryAsync<TResult>(
        QueryExpression<TResult> query,
        string? partitionKey,
        int? maxItemCount,
        string? continuationToken = default,
        CancellationToken cancellationToken = default)
        => PagedQueryAsync(query, null, partitionKey, maxItemCount, continuationToken, cancellationToken);

    /// <summary>
    /// Query documents from the configured Cosmos container using pagination and a custom result.
    /// </summary>
    /// <typeparam name="TResult">The type used for the custom query result.</typeparam>
    /// <param name="query">Cosmos query to execute.</param>
    /// <param name="options">(Optional) Query request options to use.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="maxItemCount">(Optional) The number of items to return per page.</param>
    /// <param name="continuationToken">(Optional) The continuationToken for getting the next page of a previous query.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <typeparamref name="TResult"/> containing the custom query result.</returns>
    public Task<PagedResult<TResult>> PagedQueryAsync<TResult>(
        QueryExpression<TResult> query,
        QueryRequestOptions? options,
        string? partitionKey,
        int? maxItemCount,
        string? continuationToken = default,
        CancellationToken cancellationToken = default);
}
