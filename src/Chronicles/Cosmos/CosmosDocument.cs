using System.Text.Json.Serialization;

namespace Chronicles.Cosmos;

public abstract class CosmosDocument : ICosmosDocument
{
    [JsonPropertyName(CosmosFieldNames.DocumentId)]
    public string DocumentId { get; set; } = default!;

    [JsonPropertyName(CosmosFieldNames.PartitionKey)]
    public string PartitionKey { get; set; } = default!;

    string? ICosmosDocument.ETag { get; set; }
}
