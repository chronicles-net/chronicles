using System.Diagnostics.CodeAnalysis;

namespace Chronicles.EventStore;

/// <summary>
/// The exception that is thrown when a stream is not at its expected version.
/// </summary>
[SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "By Design")]
public class StreamConflictException(
    StreamId streamId,
    StreamVersion version,
    StreamState state,
    StreamVersion? expectedVersion,
    StreamState? expectedState,
    string message)
    : Exception(message)
{
    /// <summary>
    /// The id of the stream that caused the exception.
    /// </summary>
    public StreamId StreamId { get; } = streamId;

    /// <summary>
    /// The current version of the <seealso cref="StreamConflictException.StreamId"/>.
    /// </summary>
    public StreamVersion Version { get; } = version;

    /// <summary>
    /// The current state of the stream.
    /// </summary>
    public StreamState State { get; } = state;

    /// <summary>
    /// The expected version, that caused the exception.
    /// </summary>
    public StreamVersion? ExpectedVersion { get; } = expectedVersion;

    /// <summary>
    /// The expected state of the stream, that caused the exception.
    /// </summary>
    public StreamState? ExpectedState { get; } = expectedState;
}