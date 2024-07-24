using System.Runtime.CompilerServices;
using Chronicles.Documents;
using Microsoft.Azure.Cosmos;

namespace Chronicles.EventStore.Internal;

internal class StreamMetadataReader(
    IDocumentReader<StreamMetadataDocument> reader,
    IDateTimeProvider dateTimeProvider)
    : IStreamMetadataReader
{
    public virtual async Task<StreamMetadataDocument> GetAsync(
        StreamId streamId,
        string? storeName,
        CancellationToken cancellationToken)
        => await reader
            .FindAsync(
                JsonPropertyNames.StreamMetadataId,
                (string)streamId,
                options: null,
                storeName: storeName,
                cancellationToken)
            .ConfigureAwait(false) switch
        {
            { } metadata => metadata,
            _ => new StreamMetadataDocument(
                Id: JsonPropertyNames.StreamMetadataId,
                Pk: (string)streamId,
                streamId,
                StreamState.New,
                Version: 0,
                dateTimeProvider.GetDateTime()),
        };

    public virtual async IAsyncEnumerable<StreamMetadataDocument> QueryAsync(
        string? filter,
        DateTimeOffset? createdAfter,
        string? storeName,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var evt in reader
            .QueryAsync<StreamMetadataDocument>(
                GetQueryDefinition(filter, createdAfter),
                partitionKey: null,
                options: null,
                storeName: storeName,
                cancellationToken))
        {
            yield return evt;
        }
    }

    protected virtual QueryDefinition GetQueryDefinition(
        string? filter,
        DateTimeOffset? createdAfter)
        => (filter, createdAfter) switch
        {
            ({ } f, { } c) => new QueryDefinition("SELECT * FROM c WHERE c.id == @metadataId AND c.streamId LIKE @filter AND c.timestamp > @createdAfter")
                                .WithParameter("metadataId", JsonPropertyNames.StreamMetadataId)
                                .WithParameter("filter", f)
                                .WithParameter("createdAfter", c),
            ({ } f, _) => new QueryDefinition("SELECT * FROM c WHERE c.id == @metadataId AND c.streamId LIKE @filter")
                                .WithParameter("metadataId", JsonPropertyNames.StreamMetadataId)
                                .WithParameter("filter", f),
            (_, { } c) => new QueryDefinition("SELECT * FROM c WHERE c.id == @metadataId AND c.timestamp > @createdAfter")
                                .WithParameter("metadataId", JsonPropertyNames.StreamMetadataId)
                                .WithParameter("createdAfter", c),
            _ => new QueryDefinition("SELECT * FROM c WHERE c.id == @metadataId")
                                .WithParameter("metadataId", JsonPropertyNames.StreamMetadataId),
        };
}
