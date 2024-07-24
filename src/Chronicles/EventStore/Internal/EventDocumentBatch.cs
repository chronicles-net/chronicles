namespace Chronicles.EventStore.Internal;

internal record EventDocumentBatch(
    StreamMetadataDocument Metadata,
    IReadOnlyCollection<EventDocument> Events);
