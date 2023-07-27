using System.Text.Json.Serialization;

namespace Chronicles.Cosmos;

public abstract class CosmosDocument : ICosmosDocument
{
    [JsonPropertyName("id")]
    public string DocumentId { get; set; } = default!;

    [JsonPropertyName("pk")]
    public string PartitionKey { get; set; } = default!;

    [JsonPropertyName("_etag")]
    string? ICosmosDocument.ETag { get; set; }
}