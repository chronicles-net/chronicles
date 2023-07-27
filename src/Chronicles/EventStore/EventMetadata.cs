namespace Chronicles.EventStore;

/// <summary>
/// Represents meta-data of an event in a stream.
/// </summary>
/// <param name="Name">Name of the event.</param>
/// <param name="CorrelationId">Correlation id associated with the event.</param>
/// <param name="CausationId">Causation id associated with the event.</param>
/// <param name="StreamId">Id of the stream.</param>
/// <param name="Timestamp">When the event was created.</param>
/// <param name="Version">Position within the stream</param>
public record EventMetadata(
    string Name,
    string? CorrelationId,
    string? CausationId,
    StreamId StreamId,
    DateTimeOffset Timestamp,
    StreamVersion Version)
{
    public static EventMetadata Empty
        => new(
            string.Empty,
            null,
            null,
            StreamId.Empty,
            DateTimeOffset.UtcNow,
            StreamVersion.EndOfStreamValue);
}