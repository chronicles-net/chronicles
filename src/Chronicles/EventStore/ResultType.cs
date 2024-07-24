namespace Chronicles.EventStore;

public enum ResultType
{
    /// <summary>
    /// Command resulted in successfully writing one or more events to the stream.
    /// </summary>
    Changed,

    /// <summary>
    /// Command yielded no events to write to stream.
    /// </summary>
    NotModified,

    /// <summary>
    /// Current stream version was not at the required position.
    /// </summary>
    Conflict,
}
