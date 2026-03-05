using Chronicles.Documents;

namespace Chronicles.EventStore.Internal;

internal abstract record EventDocumentBase()
    : IDocument
{
    protected abstract string GetPartitionKey();

    protected abstract string GetDocumentId();

    string IDocument.GetDocumentId()
        => GetDocumentId();

    string IDocument.GetPartitionKey()
        => GetPartitionKey();
}
