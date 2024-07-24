namespace Chronicles.EventStore.Internal;

internal interface IEventDocumentBatchProducer
{
    EventDocumentBatch FromEvents(
        IReadOnlyCollection<object> events,
        StreamMetadata metadata,
        string? storeName,
        StreamWriteOptions? options);
}