using System.Collections.Immutable;
using System.Globalization;
using System.Net;
using System.Text.Json;
using Chronicles.Documents;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using static System.FormattableString;

namespace Chronicles.Testing;

/// <summary>
/// Represents a fake <see cref="IDocumentReader{T}"/> that can be
/// used when unit testing client code.
/// </summary>
/// <typeparam name="T">
/// The type of <see cref="IDocument"/>
/// to be read by this reader.
/// </typeparam>
public class FakeDocumentReader<T> :
    IDocumentReader<T>
{
    private readonly IFakeDocumentStoreProvider storeProvider;

    public FakeDocumentReader(
        IFakeDocumentStoreProvider storeProvider)
    {
        this.storeProvider = storeProvider;
    }

    ///// <summary>
    ///// Gets or sets the list of documents to return by the fake reader.
    ///// </summary>
    //public IList<T> Documents { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of custom results to be returned by the
    /// <see cref="QueryAsync{TResult}(QueryDefinition, string, QueryRequestOptions, string?, CancellationToken)"/> method.
    /// </summary>
    public IList<object> QueryResults { get; set; } = [];

    public QueryDefinition CreateQuery<TResult>(
        QueryExpression<T, TResult> query,
        string? storeName = null)
        => new FakeQueryDefinition<T>(s => query.Invoke(s));

    public virtual Task<TResult> ReadAsync<TResult>(
        string documentId,
        string partitionKey,
        ItemRequestOptions? options,
        string? storeName = null,
        CancellationToken cancellationToken = default)
        where TResult : T
    {
        var item = storeProvider
            .GetStore(storeName)
            .GetContainer<T>()
            .GetPartition(partitionKey)
            .GetDocument(documentId);

        if (item is not TResult { } result)
        {
            throw new CosmosException(
                $"Document not found. " +
                $"Id: {documentId}. " +
                $"PartitionKey: {partitionKey}",
                HttpStatusCode.NotFound,
                0,
                string.Empty,
                0);
        }

        return Task.FromResult(
            result.DeepClone(
                GetSerializerOptions(storeName)));
    }

    public async Task<IEnumerable<TResult>> ReadManyAsync<TResult>(
        (string documentId, string partitionKey)[] ids,
        ReadManyRequestOptions? options,
        string? storeName = null,
        CancellationToken cancellationToken = default)
        where TResult : T
    {
        var items = new List<TResult>();
        foreach (var id in ids)
        {
            try
            {
                var item = await ReadAsync<TResult>(
                    id.documentId,
                    id.partitionKey,
                    null,
                    storeName,
                    cancellationToken);

                items.Add(item);
            }
            catch (CosmosException)
            {
            }
        }

        return items;
    }

    public virtual IAsyncEnumerable<TResult> QueryAsync<TResult>(
        QueryDefinition query,
        string? partitionKey,
        QueryRequestOptions? options,
        string? storeName = null,
        CancellationToken cancellationToken = default)
        => GetAsyncEnumerator(
            QueryItems<TResult>(query, partitionKey, storeName));

    public virtual Task<PagedResult<TResult>> PagedQueryAsync<TResult>(
        QueryDefinition query,
        string? partitionKey,
        int? maxItemCount,
        string? continuationToken,
        QueryRequestOptions? options,
        string? storeName = null,
        CancellationToken cancellationToken = default)
    {
        var startIndex = GetStartIndex(continuationToken);

        var items = QueryItems<TResult>(query, partitionKey, storeName)
            .Skip(startIndex)
            .Take(maxItemCount ?? 3)
            .Select(o => o.DeepClone(GetSerializerOptions(storeName)))
            .ToList();

        return Task.FromResult(new PagedResult<TResult>
        {
            Items = items,
            ContinuationToken = GetContinuationToken(startIndex, items.Count),
        });
    }

    protected static int GetStartIndex(string? continuationToken)
        => continuationToken is not null
        && int.TryParse(
            continuationToken,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var index)
        ? index
        : 0;

    protected static string? GetContinuationToken(
        int startIndex,
        int itemsCount)
        => itemsCount > 0
         ? Invariant($"{startIndex + itemsCount}")
         : null;

    protected static async IAsyncEnumerable<TItem> GetAsyncEnumerator<TItem>(
        IEnumerable<TItem> items)
    {
        foreach (var item in items)
        {
            yield return await Task
                .FromResult(item)
                .ConfigureAwait(false);
        }
    }

    protected IEnumerable<TResult> QueryItems<TResult>(
        QueryDefinition query,
        string? partitionKey,
        string? storeName)
    {
        if (query is FakeQueryDefinition<T> { LinqQuery: { } linqQuery })
        {
            return linqQuery
                .Invoke(GetDocuments(partitionKey, storeName).AsQueryable())
                .OfType<T>()
                .DeepClone<T, TResult>(GetSerializerOptions(storeName));
        }

        return QueryResults
            .OfType<TResult>()
            .DeepClone(GetSerializerOptions(storeName));
    }

    private JsonSerializerOptions GetSerializerOptions(
        string? storeName)
        => storeProvider
            .GetStore(storeName)
            .SerializerOptions;

    private ImmutableList<T> GetDocuments(
        string? partitionKey,
        string? storeName)
        => partitionKey is null
         ? [.. storeProvider
            .GetStore(storeName)
            .GetContainer<T>()
            .FromAllPartitions()
            .SelectMany(p => p.GetDocuments<T>())]
         : storeProvider
            .GetStore(storeName)
            .GetContainer<T>()
            .GetOrCreatePartition(partitionKey)
            .GetDocuments<T>();
}
