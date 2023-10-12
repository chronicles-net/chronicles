using Chronicles.Documents;

namespace Chronicles.EventStore.Internal.Streams;

internal class StreamMetadataReader
{
    private readonly IDocumentReader<StreamMetadataDocument> reader;
    private readonly IDateTimeProvider dateTimeProvider;

    public StreamMetadataReader(
        IDocumentReader<StreamMetadataDocument> reader,
        IDateTimeProvider dateTimeProvider)
    {
        this.reader = reader;
        this.dateTimeProvider = dateTimeProvider;
    }

    public virtual async Task<StreamMetadataDocument> GetAsync(
        StreamId streamId,
        CancellationToken cancellationToken)
        => await reader
            .FindAsync(
                JsonPropertyNames.StreamMetadataId,
                streamId.Value,
                options: null,
                storeName: null,
                cancellationToken)
            .ConfigureAwait(false) switch
        {
            { } metadata => metadata,
            _ => new StreamMetadataDocument(
                Id: JsonPropertyNames.StreamMetadataId,
                Pk: streamId.Value,
                streamId,
                StreamState.New,
                0,
                dateTimeProvider.GetDateTime(),
                null),
        };
}
