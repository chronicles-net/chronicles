using Chronicles.Documents;

namespace Chronicles.EventStore.Internal.Streams;

internal class StreamMetadataReader
{
    private readonly ICosmosReader<StreamMetadataDocument> reader;
    private readonly IDateTimeProvider dateTimeProvider;

    public StreamMetadataReader(
        ICosmosReader<StreamMetadataDocument> reader,
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
                cancellationToken)
            .ConfigureAwait(false) switch
        {
            { } metadata => metadata,
            _ => new StreamMetadataDocument(
                streamId.Value,
                JsonPropertyNames.StreamMetadataId,
                streamId,
                StreamState.New,
                0,
                dateTimeProvider.GetDateTime(),
                null),
        };
}
