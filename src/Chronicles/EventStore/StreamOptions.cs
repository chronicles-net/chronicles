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
}
