using System.Text.Json;
using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents.Testing
{
    public sealed class FakeCosmos<T> :
        IDocumentReader<T>,
        IDocumentWriter<T>
        where T : class, IDocument
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

        QueryDefinition IDocumentReader<T>.CreateQuery<TResult>(
            QueryExpression<T, TResult> query)
            => ((IDocumentReader<T>)Reader).CreateQuery(query);

        Task<TResult> IDocumentReader<T>.ReadAsync<TResult>(
            string documentId,
            string partitionKey,
            ItemRequestOptions? options,
            CancellationToken cancellationToken)
            where TResult : class
            => ((IDocumentReader<T>)Reader)
                .ReadAsync<TResult>(
                    documentId,
                    partitionKey,
                    options,
                    cancellationToken);

        IAsyncEnumerable<TResult> IDocumentReader<T>.QueryAsync<TResult>(
            QueryDefinition query,
            string? partitionKey,
            QueryRequestOptions? options,
            CancellationToken cancellationToken)
            => ((IDocumentReader<T>)Reader)
                .QueryAsync<TResult>(
                    query,
                    partitionKey,
                    options,
                    cancellationToken);

        Task<PagedResult<TResult>> IDocumentReader<T>.PagedQueryAsync<TResult>(
            QueryDefinition query,
            string? partitionKey,
            QueryRequestOptions? options,
            int? maxItemCount,
            string? continuationToken,
            CancellationToken cancellationToken)
            => ((IDocumentReader<T>)Reader)
                .PagedQueryAsync<TResult>(
                    query,
                    partitionKey,
                    options,
                    maxItemCount,
                    continuationToken,
                    cancellationToken);

        Task<T> IDocumentWriter<T>.CreateAsync(
            T document,
            ItemRequestOptions? options,
            CancellationToken cancellationToken)
            => ((IDocumentWriter<T>)Writer)
                .CreateAsync(
                    document,
                    options,
                    cancellationToken);

        Task<T> IDocumentWriter<T>.WriteAsync(
            T document,
            ItemRequestOptions? options,
            CancellationToken cancellationToken)
            => ((IDocumentWriter<T>)Writer)
                .WriteAsync(
                    document,
                    options,
                    cancellationToken);

        Task<T> IDocumentWriter<T>.ReplaceAsync(
            T document,
            ItemRequestOptions? options,
            CancellationToken cancellationToken)
            => ((IDocumentWriter<T>)Writer)
                .ReplaceAsync(
                    document,
                    options,
                    cancellationToken);

        Task IDocumentWriter<T>.DeleteAsync(
            string documentId,
            string partitionKey,
            ItemRequestOptions? options,
            CancellationToken cancellationToken)
            => ((IDocumentWriter<T>)Writer)
                .DeleteAsync(
                    documentId,
                    partitionKey,
                    options,
                    cancellationToken);

        Task<T> IDocumentWriter<T>.UpdateAsync(
            string documentId,
            string partitionKey,
            Func<T, Task> updateDocument,
            int retries,
            CancellationToken cancellationToken)
            => ((IDocumentWriter<T>)Writer)
                .UpdateAsync(
                    documentId,
                    partitionKey,
                    updateDocument,
                    retries,
                    cancellationToken);

        Task<T> IDocumentWriter<T>.UpdateOrCreateAsync(
            Func<T> getDefaultDocument,
            Func<T, Task> updateDocument,
            int retries,
            CancellationToken cancellationToken)
            => ((IDocumentWriter<T>)Writer)
                .UpdateOrCreateAsync(
                    getDefaultDocument,
                    updateDocument,
                    retries,
                    cancellationToken);

        public IDocumentTransaction<T> CreateTransaction(
            string partitionKey)
            => ((IDocumentWriter<T>)Writer)
                .CreateTransaction(partitionKey);
    }
}