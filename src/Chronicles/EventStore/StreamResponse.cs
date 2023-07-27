namespace Chronicles.EventStore;

public record StreamResponse(
    StreamId StreamId,
    StreamVersion Version,
    DateTimeOffset Timestamp,
    StreamState State);
