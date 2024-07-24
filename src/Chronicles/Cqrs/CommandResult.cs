using Chronicles.EventStore;

namespace Chronicles.Cqrs;

/// <summary>
/// Represents the result of a command.
/// </summary>
/// <param name="Id">Id of the stream.</param>
/// <param name="Version">Position of the stream after executing the command.</param>
/// <param name="Result">The result of executing the command.</param>
/// <param name="Response">Response returned from command handler.</param>
public record CommandResult(
    StreamId Id,
    StreamVersion Version,
    ResultType Result,
    object? Response = default);
