using System.Collections.Concurrent;

namespace Chronicles.Documents.Testing;

public class FakePartitionTransaction
{
    private readonly List<FakeTransactionalBatchOperationResult<IDocument>> operationResults = [];

    public FakePartitionTransaction(
        IFakePartitionChangeTracking changeTracking,
        string partitionKey,
        ConcurrentDictionary<string, IDocument> documents)
    {
        PartitionKey = partitionKey;
        AlteredDocuments = new ConcurrentDictionary<string, IDocument>(documents);
    }

    public ConcurrentDictionary<string, IDocument> AlteredDocuments { get; }

    public string PartitionKey { get; }

    public Task<bool> CreateDocument(
        string id,
        IDocument document)
    {
        var success = AlteredDocuments.TryAdd(id, document);
        operationResults.Add(
            new FakeTransactionalBatchOperationResult<IDocument>(
                document,
                FakeDocumentAction.Create,
                success,
                success ? System.Net.HttpStatusCode.Created : System.Net.HttpStatusCode.Conflict));

        return Task.FromResult(success);
    }

    public Task<IDocument> UpsertDocument(
        string id,
        IDocument document)
    {
        var doc = AlteredDocuments.AddOrUpdate(id, document, (key, doc) => document);
        operationResults.Add(
            new FakeTransactionalBatchOperationResult<IDocument>(
                document,
                FakeDocumentAction.Updated,
                isSuccess: true,
                System.Net.HttpStatusCode.OK));

        return Task.FromResult(doc);
    }

    public Task<bool> ReplaceDocument(
        string id,
        IDocument document)
    {
        if (AlteredDocuments.ContainsKey(id))
        {
            var doc = AlteredDocuments.AddOrUpdate(id, document, (key, doc) => document);
            operationResults.Add(
                new FakeTransactionalBatchOperationResult<IDocument>(
                    document,
                    FakeDocumentAction.Updated,
                    isSuccess: true,
                    System.Net.HttpStatusCode.OK));

            return Task.FromResult(true);
        }

        operationResults.Add(
            new FakeTransactionalBatchOperationResult<IDocument>(
                document,
                FakeDocumentAction.Updated,
                isSuccess: false,
                System.Net.HttpStatusCode.Conflict));
        return Task.FromResult(false);
    }

    public Task<bool> DeleteDocument(
        string id)
    {
        var success = AlteredDocuments.TryRemove(id, out var document);
        operationResults.Add(
            new FakeTransactionalBatchOperationResult<IDocument>(
                document,
                FakeDocumentAction.Delete,
                isSuccess: success,
                success ? System.Net.HttpStatusCode.OK : System.Net.HttpStatusCode.NotFound));

        return Task.FromResult(success);
    }

    public FakeTransactionalBatchResponse Commit()
    {
        var isSuccess = operationResults.All(os => os.IsSuccessStatusCode);
        return new FakeTransactionalBatchResponse(
            operationResults,
            isSuccess,
            isSuccess ? System.Net.HttpStatusCode.OK : System.Net.HttpStatusCode.Conflict);
    }
}

