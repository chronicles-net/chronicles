using Chronicles.EventStore;

namespace Chronicles.Cqrs;

public enum CommandConsistency
{
    /// <summary>
    /// Command events are committed to the end of the stream.
    /// </summary>
    Write,

    /// <summary>
    /// Guarantees that when events produces by the command handler are committed to the stream, the stream has not changed since reading it.
    /// Otherwise, a <see cref="StreamConflictException"/> will occur.
    /// </summary>
    ReadWrite,
}
