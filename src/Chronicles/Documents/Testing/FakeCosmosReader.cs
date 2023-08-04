using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using static System.FormattableString;

namespace Chronicles.Documents.Testing
{
    /// <summary>
    /// Represents a fake <see cref="IDocumentReader{T}"/> that can be
    /// used when unit testing client code.
    /// </summary>
    /// <typeparam name="T">
    /// The type of <see cref="IDocument"/>
    /// to be read by this reader.
    /// </typeparam>
    public class FakeCosmosReader<T> :
        IDocumentReader<T>
        where T : IDocument
    {
        private readonly JsonSerializerOptions? serializerOptions;

        public FakeCosmosReader()
        {
        }

        public FakeCosmosReader(JsonSerializerOptions serializerOptions)
        {
            this.serializerOptions = serializerOptions;
        }

        /// <summary>
        /// Gets or sets the list of documents to return by the fake reader.
        /// </summary>
        public IList<T> Documents { get; set; }
            = new List<T>();

        /// <summary>
        /// Gets or sets the list of custom results to be returned by the
        /// <see cref="QueryAsync{TResult}(QueryDefinition, string, QueryRequestOptions, CancellationToken)"/> method.
        /// </summary>
        public IList<object> QueryResults { get; set; }
            = new List<object>();

        public QueryDefinition CreateQuery<TResult>(
            QueryExpression<T, TResult> query)
            => new FakeQueryDefinition<T>(s => query.Invoke(s));

        public virtual Task<TResult?> FindAsync<TResult>(
            string documentId,
            string partitionKey,
            ItemRequestOptions? options,
            CancellationToken cancellationToken = default)
            where TResult : class, T
            => Task.FromResult(
                Documents
                    .OfType<TResult>()
                    .FirstOrDefault(d
                        => d.GetDocumentId() == documentId
                        && d.GetPartitionKey() == partitionKey)
                    ?.DeepClone(serializerOptions));

        public virtual Task<TResult> ReadAsync<TResult>(
            string documentId,
            string partitionKey,
            ItemRequestOptions? options,
            CancellationToken cancellationToken = default)
            where TResult : T
        {
            var item = Documents
                .OfType<TResult>()
                .FirstOrDefault(d
                   => d.GetDocumentId() == documentId
                   && d.GetPartitionKey() == partitionKey);

            if (item is null)
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

            return Task.FromResult(item.DeepClone(serializerOptions));
        }

        public virtual IAsyncEnumerable<T> ReadAllAsync(
            string? partitionKey,
            QueryRequestOptions? options,
            CancellationToken cancellationToken = default)
            => GetAsyncEnumerator(Documents
                .Where(d => partitionKey == null || d.GetPartitionKey() == partitionKey)
                .DeepClone(serializerOptions));

        public virtual IAsyncEnumerable<T> QueryAsync(
            QueryDefinition query,
            string? partitionKey,
            QueryRequestOptions? options,
            CancellationToken cancellationToken = default)
            => QueryAsync<T>(
                query,
                partitionKey,
                options,
                cancellationToken);

        public virtual IAsyncEnumerable<TResult> QueryAsync<TResult>(
            QueryDefinition query,
            string? partitionKey,
            QueryRequestOptions? options,
            CancellationToken cancellationToken = default)
            => GetAsyncEnumerator(
                QueryItems<TResult>(query, partitionKey));

        public virtual Task<PagedResult<T>> PagedQueryAsync(
            QueryDefinition query,
            string? partitionKey,
            QueryRequestOptions? options,
            int? maxItemCount,
            string? continuationToken = default,
            CancellationToken cancellationToken = default)
            => PagedQueryAsync<T>(
                query,
                partitionKey,
                options,
                maxItemCount,
                continuationToken,
                cancellationToken);

        public virtual Task<PagedResult<TResult>> PagedQueryAsync<TResult>(
            QueryDefinition query,
            string? partitionKey,
            QueryRequestOptions? options,
            int? maxItemCount,
            string? continuationToken = default,
            CancellationToken cancellationToken = default)
        {
            var startIndex = GetStartIndex(continuationToken);

            var items = QueryItems<TResult>(query, partitionKey)
                .Skip(startIndex)
                .Take(maxItemCount ?? 3)
                .Select(o => o.DeepClone(serializerOptions))
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
            string? partitionKey)
        {
            if (query is FakeQueryDefinition<T> { LinqQuery: { } linqQuery })
            {
                return linqQuery
                    .Invoke(Documents
                        .Where(d => partitionKey == null || d.GetPartitionKey() == partitionKey)
                        .AsQueryable())
                    .OfType<TResult>()
                    .DeepClone(serializerOptions);
            }

            return QueryResults
                .OfType<TResult>()
                .DeepClone(serializerOptions);
        }
    }
}