using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading.Tasks.Dataflow;
using Chronicles.Documents;

namespace Chronicles.Testing;

public class FakePartition(
    IFakePartitionChangeTracking changeTracking,
    string partitionKey)
{
    private readonly ConcurrentDictionary<string, IDocument> documents = [];
    private readonly BufferBlock<int> transactionBlock = new(
        new DataflowBlockOptions()
        {
            BoundedCapacity = 1,
        });

    public string PartitionKey { get; } = partitionKey;

    public ImmutableList<T> GetDocuments<T>()
        => [.. documents.Values.OfType<T>()];

    public async Task<bool> CreateDocument(
        string id,
        IDocument document)
    {
        try
        {
            await transactionBlock.SendAsync(0);

            if (documents.TryAdd(id, document))
            {
                _ = changeTracking.PartitionChangedAsync(
                    PartitionKey,
                    [new FakeDocumentOperation(FakeDocumentAction.Create, document)]);
                return true;
            }

            return false;
        }
        finally
        {
            await transactionBlock.ReceiveAsync();
        }
    }

    public async Task<IDocument> UpsertDocument(
        string id,
        IDocument document)
    {
        try
        {
            await transactionBlock.SendAsync(0);

            var doc = documents.AddOrUpdate(id, document, (key, doc) => document);

            _ = changeTracking.PartitionChangedAsync(
                PartitionKey,
                [new FakeDocumentOperation(FakeDocumentAction.Updated, document)]);

            return doc;
        }
        finally
        {
            await transactionBlock.ReceiveAsync();
        }
    }

    public async Task<bool> ReplaceDocument(
        string id,
        IDocument document)
    {
        try
        {
            await transactionBlock.SendAsync(0);

            if (documents.ContainsKey(id))
            {
                var doc = documents.AddOrUpdate(id, document, (key, doc) => document);

                _ = changeTracking.PartitionChangedAsync(
                    PartitionKey,
                    [new FakeDocumentOperation(FakeDocumentAction.Updated, document)]);

                return true;
            }

            return false;
        }
        finally
        {
            await transactionBlock.ReceiveAsync();
        }
    }

    public async Task UpsertDocuments<T>(
        ImmutableList<T> documents)
        where T : IDocument
    {
        try
        {
            await transactionBlock.SendAsync(0);

            var changes = new List<FakeDocumentOperation>();
            foreach (var document in documents)
            {
                this.documents.AddOrUpdate(
                    document.GetDocumentId(),
                    document,
                    (key, doc) => document);
                changes.Add(
                    new FakeDocumentOperation(
                        FakeDocumentAction.Updated,
                        document));
            }

            _ = changeTracking.PartitionChangedAsync(PartitionKey, [.. changes]);
        }
        finally
        {
            await transactionBlock.ReceiveAsync();
        }
    }

    public async Task<bool> DeleteDocument(string id)
    {
        try
        {
            await transactionBlock.SendAsync(0);

            if (documents.TryRemove(id, out var document))
            {
                _ = changeTracking.PartitionChangedAsync(
                    PartitionKey,
                    [new FakeDocumentOperation(FakeDocumentAction.Delete, document)]);

                return true;
            }

            return false;
        }
        finally
        {
            await transactionBlock.ReceiveAsync();
        }
    }

    public async Task DeleteAllDocument()
    {
        try
        {
            await transactionBlock.SendAsync(0);

            var all = documents.Values.ToArray();
            foreach (var document in all)
            {
                documents.Remove(document.GetDocumentId(), out _);
            }

            if (all.Length != 0)
            {
                _ = changeTracking.PartitionChangedAsync(
                    PartitionKey,
                    [.. all.Select(doc => new FakeDocumentOperation(FakeDocumentAction.Delete, doc))]);
            }
        }
        finally
        {
            await transactionBlock.ReceiveAsync();
        }
    }

    public IDocument? GetDocument(
        string id)
        => documents.TryGetValue(id, out var document)
         ? document
         : null;


    public FakePartitionTransaction CreateTransaction()
    {
        transactionBlock
            .SendAsync(0)
            .GetAwaiter()
            .GetResult();

        return new FakePartitionTransaction(
            changeTracking,
            PartitionKey,
            documents);
    }

    public async Task CommitAsync(
        FakeTransactionalBatchResponse batch)
    {
        try
        {
            if (batch.IsSuccessStatusCode == false)
            {
                return;
            }

            foreach (var result in batch.Results)
            {
                switch (result.Action)
                {
                    case FakeDocumentAction.Create:
                        documents.TryAdd(result.Resource.GetDocumentId(), result.Resource);
                        break;

                    case FakeDocumentAction.Updated:
                        documents.AddOrUpdate(
                            result.Resource.GetDocumentId(),
                            result.Resource,
                            (key, doc) => result.Resource);
                        break;

                    case FakeDocumentAction.Delete:
                        documents.TryRemove(result.Resource.GetDocumentId(), out _);
                        break;
                }
            }

            if (batch.Results.Count != 0)
            {
                _ = changeTracking.PartitionChangedAsync(
                    PartitionKey,
                    [.. batch.Results.Select(r => new FakeDocumentOperation(r.Action, r.Resource))]);
            }
        }
        finally
        {
            await transactionBlock.ReceiveAsync();
        }
    }
}

