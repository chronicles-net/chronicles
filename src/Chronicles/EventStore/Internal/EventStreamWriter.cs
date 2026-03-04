using System.Collections.Immutable;
using Chronicles.Documents;

namespace Chronicles.EventStore.Internal;

internal class EventStreamWriter(
    IDocumentWriter<IDocument> documentWriter,
    IEventDocumentWriter eventWriter,
    IStreamMetadataReader metadataReader,
    ICheckpointWriter checkpointWriter)
    : IEventStreamWriter
{
    public virtual async Task CloseAsync(
        StreamId streamId,
        string? storeName = null,
        CancellationToken cancellationToken = default)
    {
        var metadata = await metadataReader
            .GetAsync(streamId, storeName, cancellationToken)
            .ConfigureAwait(false);

        metadata.EnsureNotClosed(streamId);

        var closedDocument = StreamMetadataDocument.FromMetadata(metadata) with { State = StreamState.Closed };

        await documentWriter
            .WriteAsync(closedDocument, options: null, storeName: storeName, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public virtual async Task DeleteStreamAsync(
        StreamId streamId,
        StreamVersion? expectedVersion = null,
        string? storeName = null,
        CancellationToken cancellationToken = default)
    {
        if (expectedVersion is not null)
        {
            var metadata = await metadataReader
                .GetAsync(streamId, storeName, cancellationToken)
                .ConfigureAwait(false);

            metadata.EnsureSuccess(new StreamWriteOptions { RequiredVersion = expectedVersion });
        }

        await documentWriter
            .DeletePartitionAsync(
                partitionKey: (string)streamId,
                options: null,
                storeName: storeName,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public virtual async Task SetCheckpointAsync(
        string name,
        StreamId streamId,
        StreamVersion version,
        object? state = null,
        string? storeName = null,
        CancellationToken cancellationToken = default)
    {
        var metadata = await metadataReader
            .GetAsync(streamId, storeName, cancellationToken)
            .ConfigureAwait(false);

        metadata.EnsureCheckpointSuccess(streamId, version);

        await checkpointWriter
            .WriteAsync(name, streamId, version, state, storeName, cancellationToken)
            .ConfigureAwait(false);
    }

    public virtual async Task<StreamWriteResult> WriteAsync(
        StreamId streamId,
        IImmutableList<object> events,
        StreamWriteOptions? options = null,
        string? storeName = null,
        CancellationToken cancellationToken = default)
    {
        // if stream metadata is not provided, get it from the store
        var metadata = options?.Metadata
                    ?? await metadataReader
                        .GetAsync(streamId, storeName, cancellationToken)
                        .ConfigureAwait(false);

        metadata.EnsureSuccess(options);

        if (events.Count == 0)
        {
            return new StreamWriteResult(
                metadata,
                []);
        }

        var retries = (options?.RequiredVersion ?? StreamVersion.Any) == StreamVersion.Any
            ? 5
            : 1;
        do
        {
            try
            {
                return await eventWriter
                    .WriteStreamAsync(
                        metadata,
                        events,
                        options,
                        storeName: storeName,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (StreamConflictException)
            {
                retries--;
                if (retries <= 0)
                {
                    throw;
                }

                // Get the latest metadata and retry.
                // The current stream position (version) is important
                // to avoid insert conflict in stream.
                metadata = await metadataReader
                    .GetAsync(streamId, storeName, cancellationToken)
                    .ConfigureAwait(false);

                // Ensure we are honoring any write constrains we might have.
                metadata.EnsureSuccess(options);
            }
        }
        while (true);
    }
}
