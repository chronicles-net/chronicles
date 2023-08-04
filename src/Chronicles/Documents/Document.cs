namespace Chronicles.Documents;

/// <summary>
/// Represents a resource that can exist as a document in a Cosmos collection.
/// </summary>
public abstract class Document : IDocument
{
    string IDocument.GetDocumentId() => GetDocumentId();

    string IDocument.GetPartitionKey() => GetPartitionKey();

    protected abstract string GetDocumentId();

    protected abstract string GetPartitionKey();
}
