using System.Collections.Immutable;

namespace Chronicles.EventStore;

public record StreamWriteResult(
    StreamMetadata Metadata,
    IImmutableList<StreamEvent> Events);