using System.Collections.Immutable;
using System.Net;
using Chronicles.Documents;
using Microsoft.Azure.Cosmos;

namespace Chronicles.EventStore.Internal;

internal class EventDocumentWriter(
    IDocumentWriter<EventDocumentBase> writer,
    IEventDocumentBatchProducer batchProducer)
    : IEventDocumentWriter
{
    public async Task<StreamWriteResult> WriteStreamAsync(
        StreamMetadata metadata,
        IImmutableList<object> events,
        StreamWriteOptions? options,
        string? storeName,
        CancellationToken cancellationToken)
    {
        var batch = batchProducer
            .FromEvents(
                events,
                metadata,
                storeName,
                options);

        var transaction = writer.CreateTransaction(
            (string)metadata.StreamId,
            storeName);

        transaction.Write(
            batch.Metadata,
            new TransactionalBatchItemRequestOptions { EnableContentResponseOnWrite = true });

        foreach (var document in batch.Events)
        {
            transaction.Create(
                document,
                new TransactionalBatchItemRequestOptions { EnableContentResponseOnWrite = false });
        }

        using var result = await transaction
            .CommitAsync(cancellationToken)
            .ConfigureAwait(false);

        var newMetadata = EnsureSuccess(
            result,
            batch.Metadata.Version,
            GetMetadataFromResponse(result));

        return new StreamWriteResult(
            newMetadata,
            batch.Events
                .Select(evt => new StreamEvent(evt.Data, evt.Properties))
                .ToImmutableArray());
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
                subStatusCode: 0,
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