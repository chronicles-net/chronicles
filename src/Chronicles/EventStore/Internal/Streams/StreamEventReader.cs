using System.Runtime.CompilerServices;
using Chronicles.Cosmos;
using Microsoft.Azure.Cosmos;

namespace Chronicles.EventStore.Internal.Streams;

internal class StreamEventReader
{
    private readonly ICosmosReader<StreamEventDocument> streamReader;
    private readonly StreamMetadataReader metadataReader;

    public StreamEventReader(
        ICosmosReader<StreamEventDocument> streamReader,
        StreamMetadataReader metadataReader)
    {
        this.streamReader = streamReader;
        this.metadataReader = metadataReader;
    }

    public virtual async IAsyncEnumerable<StreamEvent> ReadAsync(
        StreamId streamId,
        StreamVersion fromVersion,
        StreamReadFilter? filter,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var metadata = await metadataReader
            .GetAsync(streamId, cancellationToken)
            .ConfigureAwait(false);

        if (filter?.RequiredVersion is { } requiredVersion
        && !metadata.Version.IsValid(requiredVersion))
        {
            throw new StreamConflictException(
                metadata.StreamId,
                metadata.Version,
                requiredVersion,
                "Conflict when reading from stream.");
        }

        // If we don't have any events in the stream, then skip reading from stream.
        if (metadata.Version.IsEmpty)
        {
            yield break;
        }

        await foreach (var evt in streamReader
            .QueryAsync<StreamEvent>(
                GetQueryDefinition(fromVersion, filter),
                streamId.Value,
                cancellationToken))
        {
            yield return evt;
        }
    }

    protected virtual QueryDefinition GetQueryDefinition(
        StreamVersion fromVersion,
        StreamReadFilter? filter)
        => streamReader.CreateQuery(
            query =>
            {
                // Exclude meta data document
                query = query.Where(e => e.Id != JsonPropertyNames.StreamMetadataId);

                if (fromVersion.IsNotEmpty)
                {
                    // Apply version restrictions.
                    // Cast to long is required for the linq provider to generate correct SQL.
                    query = query.Where(e => (long)e.Properties.Version <= (long)fromVersion);
                }

                if (filter?.IncludeEvents is { } events)
                {
                    // We need to convert it to string array as the linq provider
                    // would otherwise see it as an object with a Value property.
                    var stringEvents = events.Select(e => e.Value).ToArray();

                    query = query.Where(e => stringEvents.Contains(e.Properties.Name));
                }

                return query.OrderBy(e => e.Properties.Version);
            });
}
