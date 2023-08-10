using Chronicles.Documents;

namespace Chronicles.EventStore.Internal.Streams;

internal class StreamMetadataReader
{
    private readonly IDocumentReader<StreamDocument> reader;
    private readonly IDateTimeProvider dateTimeProvider;

    public StreamMetadataReader(
        IDocumentReader<StreamDocument> reader,
        IDateTimeProvider dateTimeProvider)
    {
        this.reader = reader;
        this.dateTimeProvider = dateTimeProvider;
    }

    public virtual async Task<StreamMetadataDocument> GetAsync(
        StreamId streamId,
        CancellationToken cancellationToken)
        => await reader
            .FindAsync<StreamDocument, StreamMetadataDocument>(
                JsonPropertyNames.StreamMetadataId,
                streamId.Value,
                options: null,
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
