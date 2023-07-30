using System.Text.Json;
using Microsoft.Azure.Cosmos;

namespace Chronicles.Cosmos.Testing
{
    public sealed class FakeCosmos<T> :
        ICosmosReader<T>,
        ICosmosWriter<T>
        where T : class, ICosmosDocument
    {
        public FakeCosmos()
            : this(
                new FakeCosmosReader<T>(),
                new FakeCosmosWriter<T>())
        {
        }

        public FakeCosmos(JsonSerializerOptions options)
            : this(
                new FakeCosmosReader<T>(options),
                new FakeCosmosWriter<T>(options))
        {
        }

        public FakeCosmos(
            FakeCosmosReader<T> reader,
            FakeCosmosWriter<T> writer)
        {
            Reader = reader;
            Writer = writer;
            writer.Documents = reader.Documents;
        }

        public IList<T> Documents
        {
            get => Reader.Documents;
            set
            {
                Reader.Documents = value;
                Writer.Documents = value;
            }
        }

        public IList<object> QueryResults
        {
            get => Reader.QueryResults;
            set
            {
                Reader.QueryResults = value;
            }
        }

        public FakeCosmosReader<T> Reader { get; }

        public FakeCosmosWriter<T> Writer { get; }

        QueryDefinition ICosmosReader<T>.CreateQuery<TResult>(
            Func<IQueryable<T>, IQueryable<TResult>> query)
            => ((ICosmosReader<T>)Reader).CreateQuery(query);

        Task<T?> ICosmosReader<T>.FindAsync(
            string documentId,
            string partitionKey,
            ItemRequestOptions? options,
            CancellationToken cancellationToken)
            => ((ICosmosReader<T>)Reader)
                .FindAsync(
                    documentId,
                    partitionKey,
                    options,
                    cancellationToken);

        Task<T> ICosmosReader<T>.ReadAsync(
            string documentId,
            string partitionKey,
            ItemRequestOptions? options,
            CancellationToken cancellationToken)
            => ((ICosmosReader<T>)Reader)
                .ReadAsync(
                    documentId,
                    partitionKey,
                    options,
                    cancellationToken);

        IAsyncEnumerable<T> ICosmosReader<T>.ReadAllAsync(
            string? partitionKey,
            QueryRequestOptions? options,
            CancellationToken cancellationToken)
            => ((ICosmosReader<T>)Reader)
                .ReadAllAsync(
                    partitionKey,
                    options,
                    cancellationToken);

        IAsyncEnumerable<TResult> ICosmosReader<T>.QueryAsync<TResult>(
            QueryDefinition query,
            string? partitionKey,
            QueryRequestOptions? options,
            CancellationToken cancellationToken)
            => ((ICosmosReader<T>)Reader)
                .QueryAsync<TResult>(
                    query,
                    partitionKey,
                    cancellationToken);

        Task<PagedResult<TResult>> ICosmosReader<T>.PagedQueryAsync<TResult>(
            QueryDefinition query,
            string? partitionKey,
            QueryRequestOptions? options,
            int? maxItemCount,
            string? continuationToken,
            CancellationToken cancellationToken)
            => ((ICosmosReader<T>)Reader)
                .PagedQueryAsync<TResult>(
                    query,
                    partitionKey,
                    options,
                    maxItemCount,
                    continuationToken,
                    cancellationToken);

        Task<T> ICosmosWriter<T>.CreateAsync(
            T document,
            ItemRequestOptions? options,
            CancellationToken cancellationToken)
            => ((ICosmosWriter<T>)Writer)
                .CreateAsync(
                    document,
                    options,
                    cancellationToken);

        Task<T> ICosmosWriter<T>.WriteAsync(
            T document,
            ItemRequestOptions? options,
            CancellationToken cancellationToken)
            => ((ICosmosWriter<T>)Writer)
                .WriteAsync(
                    document,
                    options,
                    cancellationToken);

        Task<T> ICosmosWriter<T>.ReplaceAsync(
            T document,
            ItemRequestOptions? options,
            CancellationToken cancellationToken)
            => ((ICosmosWriter<T>)Writer)
                .ReplaceAsync(
                    document,
                    options,
                    cancellationToken);

        Task ICosmosWriter<T>.DeleteAsync(
            string documentId,
            string partitionKey,
            ItemRequestOptions? options,
            CancellationToken cancellationToken)
            => ((ICosmosWriter<T>)Writer)
                .DeleteAsync(
                    documentId,
                    partitionKey,
                    options,
                    cancellationToken);

        Task<bool> ICosmosWriter<T>.TryDeleteAsync(
            string documentId,
            string partitionKey,
            ItemRequestOptions? options,
            CancellationToken cancellationToken)
            => ((ICosmosWriter<T>)Writer)
                .TryDeleteAsync(
                    documentId,
                    partitionKey,
                    options,
                    cancellationToken);

        Task<T> ICosmosWriter<T>.UpdateAsync(
            string documentId,
            string partitionKey,
            Func<T, Task> updateDocument,
            int retries,
            CancellationToken cancellationToken)
            => ((ICosmosWriter<T>)Writer)
                .UpdateAsync(
                    documentId,
                    partitionKey,
                    updateDocument,
                    retries,
                    cancellationToken);

        Task<T> ICosmosWriter<T>.UpdateOrCreateAsync(
            Func<T> getDefaultDocument,
            Func<T, Task> updateDocument,
            int retries,
            CancellationToken cancellationToken)
            => ((ICosmosWriter<T>)Writer)
                .UpdateOrCreateAsync(
                    getDefaultDocument,
                    updateDocument,
                    retries,
                    cancellationToken);

        public ICosmosTransaction<T> CreateTransaction(
            string partitionKey)
            => ((ICosmosWriter<T>)Writer)
                .CreateTransaction(partitionKey);
    }
}