using Chronicles.EventStore.Internal.Streams;

namespace Chronicles.EventStore.Internal.Events;

internal class EventDocumentBatchProducer
{
    private readonly IDateTimeProvider dateTimeProvider;
    private readonly StreamEventCatalog catalog;

    public EventDocumentBatchProducer(
        IDateTimeProvider dateTimeProvider,
        StreamEventCatalog catalog)
    {
        this.dateTimeProvider = dateTimeProvider;
        this.catalog = catalog;
    }

    public virtual StreamEventBatch FromEvents(
        IReadOnlyCollection<object> events,
        StreamMetadataDocument metadata,
        StreamWriteOptions? options)
    {
        var timestamp = dateTimeProvider.GetDateTime();
        var version = metadata.Version.Value;

        var documents = events
            .Select(evt => Convert(
                evt,
                metadata,
                ++version, // increment version for event
                options?.CorrelationId,
                options?.CausationId,
                timestamp))
            .ToArray();

        return new StreamEventBatch(
            metadata with
            {
                Version = version,
                State = StreamState.Active,
                Timestamp = timestamp,
            },
            documents);
    }

    private StreamEventDocument Convert(
        object evt,
        StreamMetadata metadata,
        long version,
        string? correlationId,
        string? causationId,
        DateTimeOffset timestamp)
        => new(
            Id: $"{version}",
            Pk: metadata.StreamId.Value,
            Data: evt,
            Properties: new EventMetadata(
                (string)catalog.GetName(evt.GetType()),
                CorrelationId: correlationId,
                CausationId: causationId,
                metadata.StreamId,
                timestamp,
                version));
}