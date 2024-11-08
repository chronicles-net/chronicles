using System.Text.Json;

namespace Chronicles.EventStore;

/// <summary>
/// Represents the context for event conversion.
/// </summary>
/// <param name="Data">The json data to convert.</param>
/// <param name="Metadata">Metadata of the event.</param>
/// <param name="Options">JSON serializer options.</param>
public record EventConverterContext(
    JsonElement Data,
    EventMetadata Metadata,
    JsonSerializerOptions Options);
