using System.Net;
using Chronicles.Cosmos;
using Chronicles.EventStore.Internal.Events;
using Microsoft.Azure.Cosmos;

namespace Chronicles.EventStore.Internal.Streams;

internal class StreamEventWriter
{
    private readonly ICosmosWriter<StreamDocument> writer;
    private readonly StreamMetadataReader metadataReader;
    private readonly EventDocumentBatchProducer batchProducer;

    public StreamEventWriter(
        ICosmosWriter<StreamDocument> writer,
        StreamMetadataReader metadataReader,
        EventDocumentBatchProducer batchProducer)
    {
        this.writer = writer;
        this.metadataReader = metadataReader;
        this.batchProducer = batchProducer;
    }

    public async Task<StreamMetadata> WriteAsync(
        StreamId streamId,
        IReadOnlyCollection<object> events,
        StreamVersion version,
        StreamWriteOptions? options,
        CancellationToken cancellationToken)
    {
        var metadata = await metadataReader
            .GetAsync(streamId, cancellationToken)
            .ConfigureAwait(false);

        if (!metadata.Version.IsValid(version))
        {
            throw new StreamVersionConflictException(
                metadata.StreamId,
                metadata.Version,
                version,
                StreamConflictReason.StreamIsNotEmpty,
                $"Stream position is {metadata.Version} but expected to be {version.Value}.");
        }

        var batch = batchProducer
            .FromEvents(
                events,
                metadata,
                options);

        var transaction = writer.CreateTransaction(streamId.Value);
        transaction
            .Replace(
                batch.Metadata,
                new() { IfMatchEtag = batch.Metadata.Etag ?? string.Empty });

        foreach (var document in batch.Events)
        {
            transaction.Create(
                document,
                new TransactionalBatchItemRequestOptions { EnableContentResponseOnWrite = false });
        }

        using var result = await transaction
            .CommitAsync(cancellationToken)
            .ConfigureAwait(false);

        EnsureSuccess(result, batch.Metadata);

        return GetMetadataFromResponse(result);
    }

    private static StreamMetadata GetMetadataFromResponse(
        TransactionalBatchResponse response)
        => response
            .GetOperationResultAtIndex<StreamMetadataDocument>(0)
            .Resource;

    private static void EnsureSuccess(
        TransactionalBatchResponse response,
        StreamMetadata metadata)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new CosmosException(
                response.ErrorMessage,
                response.StatusCode,
                0,
                response.ActivityId,
                response.RequestCharge);
        }

        throw new StreamWriteConflictException(
            metadata.StreamId,
            metadata.Version);
    }
}