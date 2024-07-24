using System.Runtime.CompilerServices;
using Chronicles.Documents;
using Microsoft.Azure.Cosmos;

namespace Chronicles.EventStore.Internal;

internal class EventDocumentReader(
    IDocumentReader<EventDocument> streamReader)
    : IEventDocumentReader
{
    public virtual async IAsyncEnumerable<StreamEvent> ReadAsync(
        StreamMetadata metadata,
        StreamReadOptions? options,
        string? storeName,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // If we don't have any events in the stream, then skip reading from stream.
        if (metadata.Version.IsEmpty)
        {
            yield break;
        }

        await foreach (var evt in streamReader
            .QueryAsync<StreamEvent>(
                GetQueryDefinition(options),
                (string)metadata.StreamId,
                options: null,
                storeName: storeName,
                cancellationToken))
        {
            if (evt.Data is StreamMetadataDocument)
            {
                continue;
            }

            yield return evt;
        }
    }

    protected virtual QueryDefinition GetQueryDefinition(
        StreamReadOptions? options)
        => streamReader.CreateQuery(
            query =>
            {
                // Exclude meta data document
                query = query.Where(e => e.Id != JsonPropertyNames.StreamMetadataId);

                if (options is { FromVersion: { } fromVersion })
                {
                    // Apply version restrictions.
                    // Cast to long is required for the linq provider to generate correct SQL.
                    query = query.Where(e => (long)e.Properties.Version <= (long)fromVersion);
                }

                if (options?.IncludeEvents is { } events)
                {
                    // We need to convert it to string array as the linq provider
                    // would otherwise see it as an object with a Value property.
                    var stringEvents = events.Select(e => e.Value).ToArray();

                    query = query.Where(e => stringEvents.Contains(e.Properties.Name));
                }

                return query.OrderBy(e => e.Properties.Version);
            });
}