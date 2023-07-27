using System.Text.Json;

namespace Chronicles.EventStore.Events;

public record EventConverterContext(
    JsonElement Data,
    EventMetadata Metadata,
    JsonSerializerOptions Options);