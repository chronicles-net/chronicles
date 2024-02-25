using System.Net;
using Chronicles.Documents;
using Chronicles.EventStore.Internal.Events;
using Microsoft.Azure.Cosmos;

namespace Chronicles.EventStore.Internal.Streams;

internal class StreamEventWriter(
    IDocumentWriter<StreamDocument> writer,
    IStreamMetadataReader metadataReader,
    EventDocumentBatchProducer batchProducer)
    : IStreamEventWriter
{
    public virtual async Task<StreamMetadata> WriteAsync(
        StreamId streamId,
        IReadOnlyCollection<object> events,
        StreamWriteOptions? options,
        string? storeName,
        CancellationToken cancellationToken)
    {
        var metadata = await metadataReader
            .GetAsync(
                streamId,
                storeName: storeName,
                cancellationToken)
            .ConfigureAwait(false);

        metadata.EnsureSuccess(options);

        var batch = batchProducer
            .FromEvents(
                events,
                metadata,
                options);

        var transaction = writer.CreateTransaction(
            (string)streamId,
            storeName);
        transaction
            .Write(
                batch.Metadata,
                new() { IfMatchEtag = batch.Metadata.Etag });

        foreach (var document in batch.Events)
        {
            transaction.Create(
                document,
                new TransactionalBatchItemRequestOptions { EnableContentResponseOnWrite = false });
        }

        using var result = await transaction
            .CommitAsync(cancellationToken)
            .ConfigureAwait(false);

        return EnsureSuccess(
            result,
            batch.Metadata.Version,
            GetMetadataFromResponse(result));
    }

    private static StreamMetadataDocument GetMetadataFromResponse(
        TransactionalBatchResponse response)
        => response
            .GetOperationResultAtIndex<StreamMetadataDocument>(0)
            .Resource;

    private static StreamMetadata EnsureSuccess(
        TransactionalBatchResponse response,
        StreamVersion expectedVersion,
        StreamMetadata metadata)
    {
        if (response.IsSuccessStatusCode)
        {
            return metadata;
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

        throw new StreamConflictException(
            metadata.StreamId,
            metadata.Version,
            metadata.State,
            expectedVersion,
            expectedState: null,
            "Conflict on writing to stream.");
    }
}