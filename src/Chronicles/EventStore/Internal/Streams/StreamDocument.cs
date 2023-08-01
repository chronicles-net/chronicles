using Chronicles.Cosmos;

namespace Chronicles.EventStore.Internal.Streams;

public abstract record StreamDocument()
    : ICosmosDocument
{
    protected abstract string GetPartitionKey();

    protected abstract string GetDocumentId();

    string ICosmosDocument.GetDocumentId()
        => GetDocumentId();

    string ICosmosDocument.GetPartitionKey()
        => GetPartitionKey();
}
