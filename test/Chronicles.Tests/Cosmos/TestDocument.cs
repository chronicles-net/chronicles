using Chronicles.Cosmos;

namespace Chronicles.Tests.Cosmos
{
    public sealed class TestDocument : ICosmosDocument
    {
        public string Id { get; set; } = string.Empty;

        public string Pk { get; set; } = string.Empty;

        public string? ETag { get; set; }

        public string Data { get; set; } = default!;

        string ICosmosDocument.GetDocumentId() => Id;

        string ICosmosDocument.GetPartitionKey() => Pk;
    }
}