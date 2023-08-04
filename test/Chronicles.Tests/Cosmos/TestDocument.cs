using Chronicles.Documents;

namespace Chronicles.Tests.Cosmos;

public class TestDocument : Document
{
    public string Id { get; set; } = string.Empty;

    public string Pk { get; set; } = string.Empty;

    public string? ETag { get; set; }

    public string Data { get; set; } = default!;

    protected override string GetDocumentId() => Id;

    protected override string GetPartitionKey() => Pk;
}

public sealed class TestDocumentSubClass : TestDocument
{
    public string? ExtraProperty { get; set; }
}