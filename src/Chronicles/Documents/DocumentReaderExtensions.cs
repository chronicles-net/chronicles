using System.Linq.Expressions;
using System.Net;
using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents;

public static class DocumentReaderExtensions
{
    /// <summary>
    /// Attempts to read the specified <typeparamref name="T"/> document,
    /// and returns <c>null</c> if none was found.
    /// </summary>
    /// <typeparam name="T">The type used by the <see cref="IDocumentReader{T}"/>.</typeparam>
    /// <param name="reader">The <see cref="IDocumentReader{T}"/>.</param>
    /// <param name="documentId">Id of the document.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <see cref="Task"/> containing the requested <typeparamref name="T"/> document, or null.</returns>
    public static Task<T?> FindAsync<T>(
        this IDocumentReader<T> reader,
        string documentId,
        string partitionKey,
        CancellationToken cancellationToken = default)
        => reader.FindAsync<T, T>(
            documentId,
            partitionKey,
            options: null,
            storeName: null,
            cancellationToken);

    /// <summary>
    /// Attempts to read the specified <typeparamref name="T"/> document,
    /// and returns <c>null</c> if none was found.
    /// </summary>
    /// <typeparam name="T">The type used by the <see cref="IDocumentReader{T}"/>.</typeparam>
    /// <param name="reader">The <see cref="IDocumentReader{T}"/>.</param>
    /// <param name="documentId">Id of the document.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="options">(Optional) Query request options to use.</param>
    /// <param name="storeName">(Optional) Name of the configured document store.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <see cref="Task"/> containing the requested <typeparamref name="T"/> document, or null.</returns>
    public static Task<T?> FindAsync<T>(
        this IDocumentReader<T> reader,
        string documentId,
        string partitionKey,
        ItemRequestOptions? options,
        string? storeName = null,
        CancellationToken cancellationToken = default)
        => reader.FindAsync<T, T>(
            documentId,
            partitionKey,
            options,
            storeName,
            cancellationToken);

    /// <summary>
    /// Attempts to read the specified <typeparamref name="T"/> document,
    /// and returns <c>null</c> if none was found.
    /// </summary>
    /// <typeparam name="T">The type used by the <see cref="IDocumentReader{T}"/>.</typeparam>
    /// <typeparam name="TResult">
    /// The type used when finding a document.
    /// This can be used when <typeparamref name="T"/> is in it self a generic type.
    /// </typeparam>
    /// <param name="reader">The <see cref="IDocumentReader{T}"/>.</param>
    /// <param name="documentId">Id of the document.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="options">(Optional) Query request options to use.</param>
    /// <param name="storeName">(Optional) Name of the configured document store.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <see cref="Task"/> containing the requested <typeparamref name="T"/> document, or null.</returns>
    public static async Task<TResult?> FindAsync<T, TResult>(
        this IDocumentReader<T> reader,
        string documentId,
        string partitionKey,
        ItemRequestOptions? options,
        string? storeName = null,
        CancellationToken cancellationToken = default)
        where TResult : T
    {
        try
        {
            return await reader
                .ReadAsync<TResult>(
                    documentId,
                    partitionKey,
                    options,
                    storeName,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (CosmosException ex)
         when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }
    }

    /// <summary>
    /// Reads the specified <typeparamref name="T"/> document from the configured
    /// Cosmos collection.
    /// </summary>
    /// <remarks>
    /// A <see cref="CosmosException"/>
    /// with StatusCode <see cref="HttpStatusCode.NotFound"/>
    /// will be thrown if document could not be found.
    /// </remarks>
    /// <typeparam name="T">The type used by the <see cref="IDocumentReader{T}"/>.</typeparam>
    /// <param name="reader">The <see cref="IDocumentReader{T}"/>.</param>
    /// <param name="documentId">Id of the document.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <see cref="Task"/> the requested <typeparamref name="T"/> document.</returns>
    public static Task<T> ReadAsync<T>(
        this IDocumentReader<T> reader,
        string documentId,
        string partitionKey,
        CancellationToken cancellationToken = default)
        => reader.ReadAsync(
            documentId,
            partitionKey,
            options: null,
            storeName: null,
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
    /// <typeparam name="T">The type used by the <see cref="IDocumentReader{T}"/>.</typeparam>
    /// <param name="reader">The <see cref="IDocumentReader{T}"/>.</param>
    /// <param name="documentId">Id of the document.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="options">(Optional) Query request options to use.</param>
    /// <param name="storeName">(Optional) Name of the configured document store.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>A <see cref="Task"/> the requested <typeparamref name="T"/> document.</returns>
    public static Task<T> ReadAsync<T>(
        this IDocumentReader<T> reader,
        string documentId,
        string partitionKey,
        ItemRequestOptions? options,
        string? storeName = null,
        CancellationToken cancellationToken = default)
        => reader.ReadAsync<T>(
            documentId,
            partitionKey,
            options,
            storeName,
            cancellationToken);

    /// <summary>
    /// Reads all the specified <typeparamref name="T"/> document from the configured
    /// Cosmos collection.
    /// </summary>
    /// <typeparam name="T">The type used by the <see cref="IDocumentReader{T}"/>.</typeparam>
    /// <param name="reader">The <see cref="IDocumentReader{T}"/>.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> over all the <typeparamref name="T"/> documents.</returns>
    public static IAsyncEnumerable<T> ReadAllAsync<T>(
        this IDocumentReader<T> reader,
        string? partitionKey,
        CancellationToken cancellationToken = default)
        => reader.ReadAllAsync(
            partitionKey,
            options: null,
            storeName: null,
            cancellationToken);

    /// <summary>
    /// Reads all the specified <typeparamref name="T"/> document from the configured
    /// Cosmos collection.
    /// </summary>
    /// <typeparam name="T">The type used by the <see cref="IDocumentReader{T}"/>.</typeparam>
    /// <param name="reader">The <see cref="IDocumentReader{T}"/>.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="options">(Optional) Query request options to use.</param>
    /// <param name="storeName">(Optional) Name of the configured document store.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> over all the <typeparamref name="T"/> documents.</returns>
    public static IAsyncEnumerable<T> ReadAllAsync<T>(
        this IDocumentReader<T> reader,
        string? partitionKey,
        QueryRequestOptions? options,
        string? storeName = null,
        CancellationToken cancellationToken = default)
        => reader.QueryAsync<T>(
            reader.CreateQuery(q => q),
            partitionKey,
            options,
            storeName,
            cancellationToken);

    /// <summary>
    /// Query documents from the configured Cosmos container.
    /// </summary>
    /// <typeparam name="T">The type used by the <see cref="IDocumentReader{T}"/>.</typeparam>
    /// <param name="reader">The <see cref="IDocumentReader{T}"/>.</param>
    /// <param name="query">Cosmos query to execute.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> over the requested <typeparamref name="T"/> documents.</returns>
    public static IAsyncEnumerable<T> QueryAsync<T>(
        this IDocumentReader<T> reader,
        QueryDefinition query,
        string? partitionKey,
        CancellationToken cancellationToken = default)
        => reader.QueryAsync<T>(
            query,
            partitionKey,
            options: null,
            storeName: null,
            cancellationToken);

    /// <summary>
    /// Query documents from the configured Cosmos container.
    /// </summary>
    /// <typeparam name="T">The type used by the <see cref="IDocumentReader{T}"/>.</typeparam>
    /// <typeparam name="TResult">The type used for the custom query result.</typeparam>
    /// <param name="reader">The <see cref="IDocumentReader{T}"/>.</param>
    /// <param name="query">The Linq query to execute.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> over the requested <typeparamref name="T"/> documents.</returns>
    public static IAsyncEnumerable<TResult> QueryAsync<T, TResult>(
        this IDocumentReader<T> reader,
        QueryExpression<T, TResult> query,
        string? partitionKey,
        CancellationToken cancellationToken = default)
        => reader.QueryAsync(
            query,
            partitionKey,
            options: null,
            storeName: null,
            cancellationToken);

    /// <summary>
    /// Query documents from the configured Cosmos container.
    /// </summary>
    /// <typeparam name="T">The type used by the <see cref="IDocumentReader{T}"/>.</typeparam>
    /// <typeparam name="TResult">The type used for the custom query result.</typeparam>
    /// <param name="reader">The <see cref="IDocumentReader{T}"/>.</param>
    /// <param name="query">The Linq query to execute.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="options">(Optional) Query request options to use.</param>
    /// <param name="storeName">(Optional) Name of the configured document store.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> over the requested <typeparamref name="T"/> documents.</returns>
    public static IAsyncEnumerable<TResult> QueryAsync<T, TResult>(
        this IDocumentReader<T> reader,
        QueryExpression<T, TResult> query,
        string? partitionKey,
        QueryRequestOptions? options,
        string? storeName = null,
        CancellationToken cancellationToken = default)
        => reader.QueryAsync<TResult>(
            reader.CreateQuery(query),
            partitionKey,
            options,
            storeName,
            cancellationToken);

    /// <summary>
    /// Query documents from the configured Cosmos container.
    /// </summary>
    /// <typeparam name="T">The type used by the <see cref="IDocumentReader{T}"/>.</typeparam>
    /// <param name="reader">The <see cref="IDocumentReader{T}"/>.</param>
    /// <param name="predicate">The predicate for selecting <typeparamref name="T"/> results.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> over the requested <typeparamref name="T"/> documents.</returns>
    public static IAsyncEnumerable<T> QueryAsync<T>(
        this IDocumentReader<T> reader,
        Expression<Func<T, bool>> predicate,
        string? partitionKey,
        CancellationToken cancellationToken = default)
        => reader.QueryAsync(
            predicate,
            partitionKey,
            options: null,
            storeName: null,
            cancellationToken);

    /// <summary>
    /// Query documents from the configured Cosmos container.
    /// </summary>
    /// <typeparam name="T">The type used by the <see cref="IDocumentReader{T}"/>.</typeparam>
    /// <param name="reader">The <see cref="IDocumentReader{T}"/>.</param>
    /// <param name="predicate">The predicate for selecting <typeparamref name="T"/> results.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="options">(Optional) Query request options to use.</param>
    /// <param name="storeName">(Optional) Name of the configured document store.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> over the requested <typeparamref name="T"/> documents.</returns>
    public static IAsyncEnumerable<T> QueryAsync<T>(
        this IDocumentReader<T> reader,
        Expression<Func<T, bool>> predicate,
        string? partitionKey,
        QueryRequestOptions? options,
        string? storeName = null,
        CancellationToken cancellationToken = default)
        => reader.QueryAsync<T>(
            reader.CreateQuery(q => q.Where(predicate)),
            partitionKey,
            options,
            storeName,
            cancellationToken);

    /// <summary>
    /// Query documents from the configured Cosmos container using pagination.
    /// </summary>
    /// <typeparam name="T">The type used by the <see cref="IDocumentReader{T}"/>.</typeparam>
    /// <param name="reader">The <see cref="IDocumentReader{T}"/>.</param>
    /// <param name="query">Cosmos query to execute.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="maxItemCount">The number of items to return per page.</param>
    /// <param name="continuationToken">(Optional) The continuationToken for getting the next page of a previous query.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> over the requested <typeparamref name="T"/> documents.</returns>
    public static Task<PagedResult<T>> PagedQueryAsync<T>(
        this IDocumentReader<T> reader,
        QueryDefinition query,
        string? partitionKey,
        int? maxItemCount,
        string? continuationToken = default,
        CancellationToken cancellationToken = default)
        => reader.PagedQueryAsync<T>(
            query,
            partitionKey,
            maxItemCount,
            continuationToken,
            options: null,
            storeName: null,
            cancellationToken);

    /// <summary>
    /// Query documents from the configured Cosmos container using pagination.
    /// </summary>
    /// <typeparam name="T">The type used by the <see cref="IDocumentReader{T}"/>.</typeparam>
    /// <typeparam name="TResult">The type used for the custom query result.</typeparam>
    /// <param name="reader">The <see cref="IDocumentReader{T}"/>.</param>
    /// <param name="query">Cosmos query to execute.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="maxItemCount">The number of items to return per page.</param>
    /// <param name="continuationToken">(Optional) The continuationToken for getting the next page of a previous query.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> over the requested <typeparamref name="T"/> documents.</returns>
    public static Task<PagedResult<TResult>> PagedQueryAsync<T, TResult>(
        this IDocumentReader<T> reader,
        QueryExpression<T, TResult> query,
        string? partitionKey,
        int? maxItemCount,
        string? continuationToken = default,
        CancellationToken cancellationToken = default)
        => reader.PagedQueryAsync(
            query,
            partitionKey,
            maxItemCount,
            continuationToken,
            options: null,
            storeName: null,
            cancellationToken);

    /// <summary>
    /// Query documents from the configured Cosmos container using pagination.
    /// </summary>
    /// <typeparam name="T">The type used by the <see cref="IDocumentReader{T}"/>.</typeparam>
    /// <typeparam name="TResult">The type used for the custom query result.</typeparam>
    /// <param name="reader">The <see cref="IDocumentReader{T}"/>.</param>
    /// <param name="query">Cosmos query to execute.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="maxItemCount">The number of items to return per page.</param>
    /// <param name="continuationToken">(Optional) The continuationToken for getting the next page of a previous query.</param>
    /// <param name="options">(Optional) Query request options to use.</param>
    /// <param name="storeName">(Optional) Name of the configured document store.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> over the requested <typeparamref name="T"/> documents.</returns>
    public static Task<PagedResult<TResult>> PagedQueryAsync<T, TResult>(
        this IDocumentReader<T> reader,
        QueryExpression<T, TResult> query,
        string? partitionKey,
        int? maxItemCount,
        string? continuationToken,
        QueryRequestOptions? options,
        string? storeName = null,
        CancellationToken cancellationToken = default)
        => reader.PagedQueryAsync<TResult>(
            reader.CreateQuery(query),
            partitionKey,
            maxItemCount,
            continuationToken,
            options,
            storeName,
            cancellationToken);

    /// <summary>
    /// Query documents from the configured Cosmos container using pagination.
    /// </summary>
    /// <typeparam name="T">The type used by the <see cref="IDocumentReader{T}"/>.</typeparam>
    /// <param name="reader">The <see cref="IDocumentReader{T}"/>.</param>
    /// <param name="predicate">The predicate for selecting <typeparamref name="T"/> results.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="maxItemCount">The number of items to return per page.</param>
    /// <param name="continuationToken">(Optional) The continuationToken for getting the next page of a previous query.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> over the requested <typeparamref name="T"/> documents.</returns>
    public static Task<PagedResult<T>> PagedQueryAsync<T>(
        this IDocumentReader<T> reader,
        Expression<Func<T, bool>> predicate,
        string? partitionKey,
        int? maxItemCount,
        string? continuationToken = default,
        CancellationToken cancellationToken = default)
        => reader.PagedQueryAsync(
            predicate,
            partitionKey,
            maxItemCount,
            continuationToken,
            options: null,
            storeName: null,
            cancellationToken);

    /// <summary>
    /// Query documents from the configured Cosmos container using pagination.
    /// </summary>
    /// <typeparam name="T">The type used by the <see cref="IDocumentReader{T}"/>.</typeparam>
    /// <param name="reader">The <see cref="IDocumentReader{T}"/>.</param>
    /// <param name="predicate">The predicate for selecting <typeparamref name="T"/> results.</param>
    /// <param name="partitionKey">Partition key of the document.</param>
    /// <param name="maxItemCount">The number of items to return per page.</param>
    /// <param name="continuationToken">(Optional) The continuationToken for getting the next page of a previous query.</param>
    /// <param name="options">(Optional) Query request options to use.</param>
    /// <param name="storeName">(Optional) Name of the configured document store.</param>
    /// <param name="cancellationToken">(Optional) <seealso cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> over the requested <typeparamref name="T"/> documents.</returns>
    public static Task<PagedResult<T>> PagedQueryAsync<T>(
        this IDocumentReader<T> reader,
        Expression<Func<T, bool>> predicate,
        string? partitionKey,
        int? maxItemCount,
        string? continuationToken,
        QueryRequestOptions? options,
        string? storeName = null,
        CancellationToken cancellationToken = default)
        => reader.PagedQueryAsync<T>(
            reader.CreateQuery(q => q.Where(predicate)),
            partitionKey,
            maxItemCount,
            continuationToken,
            options,
            storeName,
            cancellationToken);
}
