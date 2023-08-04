using Chronicles.Documents;

namespace Chronicles.Tests.Cosmos;

public sealed class TestDocument<T> : IDocument
{
    public string Id { get; set; } = string.Empty;

    public string Pk { get; set; } = string.Empty;

    public string? ETag { get; set; }

    public T? Data { get; set; }

    string IDocument.GetDocumentId() => Id;

    string IDocument.GetPartitionKey() => Pk;
}