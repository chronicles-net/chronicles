namespace Chronicles.EventStore;

/// <summary>
/// Represents an event read from a stream.
/// </summary>
/// <param name="Data">Concreate event data.</param>
/// <param name="Metadata">Meta-data on the specific event.</param>
public record StreamEvent(
    object Data,
    EventMetadata Metadata);
