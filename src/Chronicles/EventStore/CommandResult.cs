namespace Chronicles.EventStore;

public record CommandResult(
    StreamId Id,
    StreamVersion Version,
    ResultType Result,
    object? Response = default);
