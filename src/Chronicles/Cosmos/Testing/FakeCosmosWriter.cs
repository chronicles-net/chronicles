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
            newDocument.ETag = Guid.NewGuid().ToString();
            Documents.Add(newDocument);
            return Task.FromResult(newDocument);
        }

        public virtual Task<T> WriteAsync(
            T document,
            ItemRequestOptions? options,
            CancellationToken cancellationToken = default)
        {
            RemoveAll(d
                => d.DocumentId == document.DocumentId
                && d.PartitionKey == document.PartitionKey);

            var newDocument = document.DeepClone(serializerOptions);
            newDocument.ETag = Guid.NewGuid().ToString();
            Documents.Add(newDocument);

            return Task.FromResult(newDocument);
        }

        public virtual Task<T> ReplaceAsync(
            T document,
            ItemRequestOptions? options,
            CancellationToken cancellationToken = default)
        {
            GuardExistsWithEtag(document);

            RemoveAll(d
                => d.DocumentId == document.DocumentId
                && d.PartitionKey == document.PartitionKey);

            var newDocument = document.DeepClone(serializerOptions);
            newDocument.ETag = Guid.NewGuid().ToString();
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
                => d.DocumentId == documentId
                && d.PartitionKey == partitionKey);

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
            newDocument.ETag = Guid.NewGuid().ToString();

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
                => d.DocumentId == defaultDocument.DocumentId
                && d.PartitionKey == defaultDocument.PartitionKey);

            var newDocument = (existingDocument ?? defaultDocument).DeepClone(serializerOptions);
            updateDocument(newDocument);

            newDocument.ETag = Guid.NewGuid().ToString();
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
                => d.DocumentId == document.DocumentId
                && d.PartitionKey == document.PartitionKey);

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

        protected void GuardExistsWithEtag(ICosmosDocument document)
        {
            var existingDocument = GuardExists(document);
            if (existingDocument.ETag != document.ETag)
            {
                throw new CosmosException(
                    $"Document ETag does not match, " +
                    $"indicating incorrecty document version.",
                    HttpStatusCode.PreconditionFailed,
                    0,
                    string.Empty,
                    0);
            }
        }

        protected T GuardExists(ICosmosDocument document)
            => GuardExists(document.DocumentId, document.PartitionKey);

        protected T GuardExists(
            string documentId,
            string partitionKey)
        {
            var item = Documents.FirstOrDefault(d
                => d.DocumentId == documentId
                && d.PartitionKey == partitionKey);

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