namespace Chronicles.EventStore;

/// <summary>
/// Represents an unknown event read from a stream.
/// </summary>
/// <remarks>Inspect the json content to identify the unknown event name.</remarks>
/// <param name="Json">Event data json.</param>
public record UnknownEvent(
    string Json);
