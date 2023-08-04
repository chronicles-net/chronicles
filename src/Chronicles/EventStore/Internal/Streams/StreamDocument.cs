using Chronicles.Documents;

namespace Chronicles.EventStore.Internal.Streams;

public abstract record StreamDocument()
    : IDocument
{
    protected abstract string GetPartitionKey();

    protected abstract string GetDocumentId();

    string IDocument.GetDocumentId()
        => GetDocumentId();

    string IDocument.GetPartitionKey()
        => GetPartitionKey();
}
