using Chronicles.EventStore.Internal;

namespace Chronicles.EventStore;

/// <summary>
/// Represents meta data of a given stream.
/// </summary>
/// <param name="StreamId">id of the stream</param>
/// <param name="State">State of stream</param>
/// <param name="Version">Last version written to the stream</param>
/// <param name="Timestamp">When was the stream last updated</param>
public abstract record StreamMetadata(
    StreamId StreamId,
    StreamState State,
    StreamVersion Version,
    DateTimeOffset Timestamp)
    : EventDocumentBase();
