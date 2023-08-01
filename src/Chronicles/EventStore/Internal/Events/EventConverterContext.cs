using System.Text.Json;

namespace Chronicles.EventStore.Internal.Events;

public record EventConverterContext(
    JsonElement Data,
    EventMetadata Metadata,
    JsonSerializerOptions Options);