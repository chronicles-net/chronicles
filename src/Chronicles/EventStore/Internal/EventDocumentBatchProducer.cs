namespace Chronicles.EventStore.Internal;

internal class EventDocumentBatchProducer(
    TimeProvider dateTimeProvider,
    IEventCatalogFactory eventCatalogFactory)
    : IEventDocumentBatchProducer
{
    public EventDocumentBatch FromEvents(
        IReadOnlyCollection<object> events,
        StreamMetadata metadata,
        string? storeName,
        StreamWriteOptions? options)
    {
        // It is important the each event and metadata have the same time stamp.
        var timestamp = dateTimeProvider.GetUtcNow();
        var version = metadata.Version.Value;
        var catalog = eventCatalogFactory.Get(storeName);

        var documents = events
            .Select(evt => Convert(
                evt,
                catalog.GetEventName(evt.GetType()),
                metadata,
                ++version, // increment version for event
                options?.CorrelationId,
                options?.CausationId,
                timestamp))
            .ToArray();

        return new EventDocumentBatch(
            StreamMetadataDocument.FromMetadata(metadata) with
            {
                Version = version,
                State = StreamState.Active,
                Timestamp = timestamp,
            },
            documents);
    }

    private static EventDocument Convert(
        object evt,
        string evtName,
        StreamMetadata metadata,
        long version,
        string? correlationId,
        string? causationId,
        DateTimeOffset timestamp)
        => new(
            Id: $"{version}",
            Pk: (string)metadata.StreamId,
            Data: evt,
            Properties: new EventMetadata(
                evtName,
                CorrelationId: correlationId,
                CausationId: causationId,
                metadata.StreamId,
                timestamp,
                version));
}