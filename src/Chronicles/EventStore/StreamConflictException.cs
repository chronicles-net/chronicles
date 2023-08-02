using System.Diagnostics.CodeAnalysis;

namespace Chronicles.EventStore;

/// <summary>
/// The exception that is thrown when a stream is not at its expected version.
/// </summary>
[SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "By Design")]
[SuppressMessage("Major Code Smell", "S3925:\"ISerializable\" should be implemented correctly", Justification = "By Design")]
public class StreamConflictException : Exception
{
    public StreamConflictException(
        StreamId streamId,
        StreamVersion version,
        StreamVersion expectedVersion,
        string message)
        : base(message)
    {
        StreamId = streamId;
        Version = version;
        ExpectedVersion = expectedVersion;
    }

    /// <summary>
    /// The id of the stream that caused the exception.
    /// </summary>
    public StreamId StreamId { get; }

    /// <summary>
    /// The current version of the <seealso cref="StreamConflictException.StreamId"/>.
    /// </summary>
    public StreamVersion Version { get; }

    /// <summary>
    /// The expected version, that caused the exception.
    /// </summary>
    public StreamVersion ExpectedVersion { get; }
}