namespace Chronicles.EventStore;

public class StreamReadOptions : StreamOptions
{
    /// <summary>
    /// Gets or sets the type of events to read from the stream.
    /// </summary>
    public IReadOnlyCollection<EventName>? IncludeEvents { get; set; }

    /// <summary>
    /// (Optional) Start reading stream from a given version.
    /// </summary>
    public StreamVersion? FromVersion { get; set; }
}