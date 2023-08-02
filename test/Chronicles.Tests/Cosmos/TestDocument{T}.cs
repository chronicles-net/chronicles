using Chronicles.Cosmos;

namespace Chronicles.Tests.Cosmos;

public sealed class TestDocument<T> : ICosmosDocument
{
    public string Id { get; set; } = string.Empty;

    public string Pk { get; set; } = string.Empty;

    public string? ETag { get; set; }

    public T Data { get; set; }

    string ICosmosDocument.GetDocumentId() => Id;

    string ICosmosDocument.GetPartitionKey() => Pk;
}