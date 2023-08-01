namespace Chronicles.EventStore;

/// <summary>
/// The reason for a <seealso cref="StreamVersionConflictException"/>.
/// </summary>
public enum StreamConflictReason
{
    /// <summary>
    /// No conflict detected.
    /// </summary>
    None = 0,

    /// <summary>
    /// Stream is expected to be empty but one or more events ware found.
    /// </summary>
    StreamIsNotEmpty,

    /// <summary>
    /// Stream is expected to contain events but an empty stream is found.
    /// </summary>
    StreamIsEmpty,

    /// <summary>
    /// Stream is not at the expected version.
    /// </summary>
    ExpectedVersion,
}