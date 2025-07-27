using Chronicles.EventStore;

namespace Chronicles.Cqrs;

/// <summary>
/// Specifies the consistency mode for command event handling in the event stream.
/// </summary>
public enum CommandConsistency
{
    /// <summary>
    /// Command events are appended to the end of the stream without checking for stream changes.
    /// </summary>
    Write,

    /// <summary>
    /// Ensures that when events produced by the command handler are committed to the stream, the stream has not changed since it was read.
    /// If the stream has changed, a <see cref="StreamConflictException"/> will be thrown.
    /// </summary>
    ReadWrite,
}
