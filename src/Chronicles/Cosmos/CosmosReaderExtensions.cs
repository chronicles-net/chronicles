using System.Net;
using Microsoft.Azure.Cosmos;

namespace Chronicles.Cosmos;

public static class CosmosReaderExtensions
{
    /// <summary>
    /// Attempts to read the specified <typeparamref name="T"/> document,
    /// and returns <c>null</c> if none was found.
    /// </summary>
    /// <typeparam name="T">The type used by the <see cref="ICosmosReader{T}"/>.</typeparam>
    /// <param name="reader">The <see cref="ICosmosReader{T}"/>.</param>
    /// <param name="documentId">Id of the document.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <see cref="Task"/> containing the requested <typeparamref name="T"/> document, or null.</returns>
    public static Task<T?> FindAsync<T>(
        this ICosmosReader<T> reader,
        string documentId,
        string partitionKey,
        CancellationToken cancellationToken = default)
        where T : class
        => reader.FindAsync(
            documentId,
            partitionKey,
            options: null,
            cancellationToken);

    /// <summary>
    /// Attempts to read the specified <typeparamref name="T"/> document,
    /// and returns <c>null</c> if none was found.
    /// </summary>
    /// <typeparam name="T">The type used by the <see cref="ICosmosReader{T}"/>.</typeparam>
    /// <param name="reader">The <see cref="ICosmosReader{T}"/>.</param>
    /// <param name="documentId">Id of the document.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="options">(Optional) Query request options to use.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <see cref="Task"/> containing the requested <typeparamref name="T"/> document, or null.</returns>
    public static Task<T?> FindAsync<T>(
        this ICosmosReader<T> reader,
        string documentId,
        string partitionKey,
        ItemRequestOptions? options,
        CancellationToken cancellationToken = default)
        where T : class
        => reader.FindAsync<T>(
            documentId,
            partitionKey,
            options,
            cancellationToken);

    /// <summary>
    /// Reads the specified <typeparamref name="T"/> document from the configured
    /// Cosmos collection.
    /// </summary>
    /// <remarks>
    /// A <see cref="CosmosException"/>
    /// with StatusCode <see cref="HttpStatusCode.NotFound"/>
    /// will be thrown if document could not be found.
    /// </remarks>
    /// <typeparam name="T">The type used by the <see cref="ICosmosReader{T}"/>.</typeparam>
    /// <param name="reader">The <see cref="ICosmosReader{T}"/>.</param>
    /// <param name="documentId">Id of the document.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <see cref="Task"/> the requested <typeparamref name="T"/> document.</returns>
    public static Task<T> ReadAsync<T>(
        this ICosmosReader<T> reader,
        string documentId,
        string partitionKey,
        CancellationToken cancellationToken = default)
        where T : class
        => reader.ReadAsync(
            documentId,
            partitionKey,
            options: null,
            cancellationToken);

    /// <summary>
    /// Reads the specified <typeparamref name="T"/> document from the configured
    /// Cosmos collection.
    /// </summary>
    /// <remarks>
    /// A <see cref="CosmosException"/>
    /// with StatusCode <see cref="HttpStatusCode.NotFound"/>
    /// will be thrown if document could not be found.
    /// </remarks>
    /// <typeparam name="T">The type used by the <see cref="ICosmosReader{T}"/>.</typeparam>
    /// <param name="reader">The <see cref="ICosmosReader{T}"/>.</param>
    /// <param name="documentId">Id of the document.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="options">(Optional) Query request options to use.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <see cref="Task"/> the requested <typeparamref name="T"/> document.</returns>
    public static Task<T> ReadAsync<T>(
        this ICosmosReader<T> reader,
        string documentId,
        string partitionKey,
        ItemRequestOptions? options,
        CancellationToken cancellationToken = default)
        where T : class
        => reader.ReadAsync<T>(
            documentId,
            partitionKey,
            options,
            cancellationToken);

    /// <summary>
    /// Reads all the specified <typeparamref name="T"/> document from the configured
    /// Cosmos collection.
    /// </summary>
    /// <typeparam name="T">The type used by the <see cref="ICosmosReader{T}"/>.</typeparam>
    /// <param name="reader">The <see cref="ICosmosReader{T}"/>.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>An <see cref="IAsyncEnumerable&lt;T&gt;"/> over all the <typeparamref name="T"/> documents.</returns>
    public static IAsyncEnumerable<T> ReadAllAsync<T>(
        this ICosmosReader<T> reader,
        string? partitionKey,
        CancellationToken cancellationToken = default)
        where T : class
        => reader.ReadAllAsync(
            partitionKey,
            options: null,
            cancellationToken);

    /// <summary>
    /// Reads all the specified <typeparamref name="T"/> document from the configured
    /// Cosmos collection.
    /// </summary>
    /// <typeparam name="T">The type used by the <see cref="ICosmosReader{T}"/>.</typeparam>
    /// <param name="reader">The <see cref="ICosmosReader{T}"/>.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="options">(Optional) Query request options to use.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>An <see cref="IAsyncEnumerable&lt;T&gt;"/> over all the <typeparamref name="T"/> documents.</returns>
    public static IAsyncEnumerable<T> ReadAllAsync<T>(
        this ICosmosReader<T> reader,
        string? partitionKey,
        QueryRequestOptions? options,
        CancellationToken cancellationToken = default)
        where T : class
        => reader.QueryAsync<T>(
            reader.CreateQuery(q => q),
            partitionKey,
            options,
            cancellationToken);

    /// <summary>
    /// Query documents from the configured Cosmos container.
    /// </summary>
    /// <typeparam name="T">The type used by the <see cref="ICosmosReader{T}"/>.</typeparam>
    /// <param name="reader">The <see cref="ICosmosReader{T}"/>.</param>
    /// <param name="query">Cosmos query to execute.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>An <see cref="IAsyncEnumerable&lt;T&gt;"/> over the requested <typeparamref name="T"/> documents.</returns>
    public static IAsyncEnumerable<T> QueryAsync<T>(
        this ICosmosReader<T> reader,
        QueryDefinition query,
        string? partitionKey,
        CancellationToken cancellationToken = default)
        where T : class
        => reader.QueryAsync<T>(
            query,
            partitionKey,
            options: null,
            cancellationToken);

    /// <summary>
    /// Query documents from the configured Cosmos container using pagination.
    /// </summary>
    /// <typeparam name="T">The type used by the <see cref="ICosmosReader{T}"/>.</typeparam>
    /// <param name="reader">The <see cref="ICosmosReader{T}"/>.</param>
    /// <param name="query">Cosmos query to execute.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="maxItemCount">The number of items to return per page.</param>
    /// <param name="continuationToken">(Optional) The continuationToken for getting the next page of a previous query.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>An <see cref="IAsyncEnumerable&lt;T&gt;"/> over the requested <typeparamref name="T"/> documents.</returns>
    public static Task<PagedResult<T>> PagedQueryAsync<T>(
        this ICosmosReader<T> reader,
        QueryDefinition query,
        string? partitionKey,
        int? maxItemCount,
        string? continuationToken = default,
        CancellationToken cancellationToken = default)
        where T : class
        => reader.PagedQueryAsync<T>(
            query,
            partitionKey,
            options: null,
            maxItemCount,
            continuationToken,
            cancellationToken);
}
