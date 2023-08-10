using Chronicles.EventStore.Internal.Streams;

namespace Chronicles.EventStore.Internal.Events;

internal class EventDocumentBatchProducer
{
    private readonly IDateTimeProvider dateTimeProvider;

    public EventDocumentBatchProducer(
        IDateTimeProvider dateTimeProvider)
        => this.dateTimeProvider = dateTimeProvider;

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

    private static StreamEventDocument Convert(
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
                "unknown",
                CorrelationId: correlationId,
                CausationId: causationId,
                metadata.StreamId,
                timestamp,
                version));
}