using System.Net;
using System.Text.Json;
using Microsoft.Azure.Cosmos;

namespace Chronicles.Cosmos.Testing
{
    /// <summary>
    /// Represents a fake <see cref="ICosmosWriter{T}"/> that can be
    /// used when unit testing client code.
    /// </summary>
    /// <typeparam name="T">
    /// The type of <see cref="ICosmosDocument"/>
    /// to be read by this reader.
    /// </typeparam>
    public class FakeCosmosWriter<T> :
        ICosmosWriter<T>
        where T : class, ICosmosDocument
    {
        private readonly JsonSerializerOptions? serializerOptions;

        public FakeCosmosWriter()
        {
        }

        public FakeCosmosWriter(JsonSerializerOptions serializerOptions)
        {
            this.serializerOptions = serializerOptions;
        }

        /// <summary>
        /// Gets or sets the list of documents to be modified by the fake writer.
        /// </summary>
        public IList<T> Documents { get; set; }
            = new List<T>();

        public ICosmosTransaction<T> CreateTransaction(string partitionKey)
            => new FakeCosmosTransaction<T>(this, partitionKey);

        public virtual Task<T> CreateAsync(
            T document,
            ItemRequestOptions? options,
            CancellationToken cancellationToken = default)
        {
            GuardNotExists(document);

            T newDocument = document.DeepClone(serializerOptions);
            Documents.Add(newDocument);
            return Task.FromResult(newDocument);
        }

        public virtual Task<T> WriteAsync(
            T document,
            ItemRequestOptions? options,
            CancellationToken cancellationToken = default)
        {
            RemoveAll(d
                => d.GetDocumentId() == document.GetDocumentId()
                && d.GetPartitionKey() == document.GetPartitionKey());

            var newDocument = document.DeepClone(serializerOptions);
            Documents.Add(newDocument);

            return Task.FromResult(newDocument);
        }

        public virtual Task<T> ReplaceAsync(
            T document,
            ItemRequestOptions? options,
            CancellationToken cancellationToken = default)
        {
            GuardExists(document);

            RemoveAll(d
                => d.GetDocumentId() == document.GetDocumentId()
                && d.GetPartitionKey() == document.GetPartitionKey());

            var newDocument = document.DeepClone(serializerOptions);
            Documents.Add(newDocument);

            return Task.FromResult(newDocument);
        }

        public virtual Task DeleteAsync(
            string documentId,
            string partitionKey,
            ItemRequestOptions? options,
            CancellationToken cancellationToken = default)
        {
            GuardExists(documentId, partitionKey);

            RemoveAll(d
                => d.GetDocumentId() == documentId
                && d.GetPartitionKey() == partitionKey);

            return Task.CompletedTask;
        }

        public async Task<bool> TryDeleteAsync(
            string documentId,
            string partitionKey,
            ItemRequestOptions? options,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await DeleteAsync(
                    documentId,
                    partitionKey,
                    options,
                    cancellationToken)
                .ConfigureAwait(false);
            }
            catch (CosmosException ex)
             when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }

            return true;
        }

        public virtual Task<T> UpdateAsync(
            string documentId,
            string partitionKey,
            Func<T, Task> updateDocument,
            int retries = 0,
            CancellationToken cancellationToken = default)
        {
            var document = GuardExists(documentId, partitionKey);

            var newDocument = document.DeepClone(serializerOptions);
            updateDocument(newDocument);

            Documents.Remove(document);
            Documents.Add(newDocument);

            return Task.FromResult(newDocument);
        }

        public virtual Task<T> UpdateOrCreateAsync(
            Func<T> getDefaultDocument,
            Func<T, Task> updateDocument,
            int retries = 0,
            CancellationToken cancellationToken = default)
        {
            var defaultDocument = getDefaultDocument();
            var existingDocument = Documents.FirstOrDefault(d
                => d.GetDocumentId() == defaultDocument.GetDocumentId()
                && d.GetPartitionKey() == defaultDocument.GetPartitionKey());

            var newDocument = (existingDocument ?? defaultDocument).DeepClone(serializerOptions);
            updateDocument(newDocument);

            if (existingDocument is not null)
            {
                Documents.Remove(existingDocument);
            }

            Documents.Add(newDocument);

            return Task.FromResult(newDocument);
        }

        protected void GuardNotExists(
            ICosmosDocument document)
        {
            var existingDocument = Documents.FirstOrDefault(d
                => d.GetDocumentId() == document.GetDocumentId()
                && d.GetPartitionKey() == document.GetPartitionKey());

            if (existingDocument is not null)
            {
                throw new CosmosException(
                    $"Document already exists.",
                    HttpStatusCode.Conflict,
                    0,
                    string.Empty,
                    0);
            }
        }

        protected T GuardExists(ICosmosDocument document)
            => GuardExists(document.GetDocumentId(), document.GetPartitionKey());

        protected T GuardExists(
            string documentId,
            string partitionKey)
        {
            var item = Documents.FirstOrDefault(d
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

            return item;
        }

        private void RemoveAll(Func<T, bool> predicate)
        {
            var items = Documents.Where(predicate).ToArray();
            foreach (var item in items)
            {
                Documents.Remove(item);
            }
        }
    }
}