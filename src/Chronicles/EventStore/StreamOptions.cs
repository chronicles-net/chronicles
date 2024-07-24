namespace Chronicles.EventStore;

public abstract class StreamOptions
{
    /// <summary>
    /// Gets or sets the required version the stream must be at.
    /// </summary>
    public StreamVersion? RequiredVersion { get; set; }

    /// <summary>
    /// Gets or sets the required state of stream.
    /// </summary>
    public StreamState? RequiredState { get; set; }

    /// <summary>
    /// Gets or sets the current stream state.
    /// </summary>
    /// <remarks>
    /// When provided, the underlying operations will use this
    /// to determine the current state of the stream.
    /// </remarks>
    public StreamMetadata? Metadata { get; set; }
}
