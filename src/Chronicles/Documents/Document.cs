namespace Chronicles.Documents;

public abstract class Document : IDocument
{
    string IDocument.GetDocumentId() => GetDocumentId();

    string IDocument.GetPartitionKey() => GetPartitionKey();

    protected abstract string GetDocumentId();

    protected abstract string GetPartitionKey();
}
