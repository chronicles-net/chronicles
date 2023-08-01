namespace Chronicles.EventStore.Internal.Streams;

internal record StreamEventBatch(
    StreamMetadataDocument Metadata,
    IReadOnlyCollection<StreamEventDocument> Events);
