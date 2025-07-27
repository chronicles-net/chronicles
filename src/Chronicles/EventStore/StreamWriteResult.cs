using System.Collections.Immutable;

namespace Chronicles.EventStore;

/// <summary>
/// Represents the result of writing events to an event stream, including updated stream metadata and the list of events written.
/// Use this record to return the outcome of a stream write operation, such as after appending or saving events.
/// </summary>
/// <param name="Metadata">The updated metadata for the stream after the write operation.</param>
/// <param name="Events">The list of events that were written to the stream.</param>
public record StreamWriteResult(
    StreamMetadata Metadata,
    IImmutableList<StreamEvent> Events);
