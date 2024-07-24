using System.Text.Json;

namespace Chronicles.EventStore;

public record EventConverterContext(
    JsonElement Data,
    EventMetadata Metadata,
    JsonSerializerOptions Options);