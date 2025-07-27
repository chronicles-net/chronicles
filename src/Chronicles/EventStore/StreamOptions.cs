namespace Chronicles.EventStore;

/// <summary>
/// Provides configuration options for reading and writing event streams in Chronicles.
/// Use this abstract class to specify requirements for stream version, state, and metadata when performing operations on event streams.
/// </summary>
public abstract class StreamOptions
{
    /// <summary>
    /// Gets or sets the required version the stream must be at before performing an operation.
    /// Set this property to enforce optimistic concurrency or to ensure the stream is at a specific version.
    /// </summary>
    public StreamVersion? RequiredVersion { get; set; }

    /// <summary>
    /// Gets or sets the required state of the stream before performing an operation.
    /// Set this property to ensure the stream is in a specific state, such as active, closed, or archived.
    /// </summary>
    public StreamState? RequiredState { get; set; }

    /// <summary>
    /// Gets or sets the current stream metadata.
    /// When provided, underlying operations will use this to determine the current state and properties of the stream.
    /// </summary>
    public StreamMetadata? Metadata { get; set; }
}
