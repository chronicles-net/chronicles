using System.Collections.Immutable;
using System.Net;
using Chronicles.Documents;
using Microsoft.Azure.Cosmos;

namespace Chronicles.EventStore.Internal;

internal class EventDocumentWriter(
    IDocumentWriter<IDocument> writer,
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
            new TransactionalBatchItemRequestOptions { EnableContentResponseOnWrite = false });

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
            batch.Metadata);

        return new StreamWriteResult(
            newMetadata,
            batch.Events
                .Select(evt => new StreamEvent(evt.Data, evt.Properties))
                .ToImmutableArray());
    }

    private static StreamMetadata EnsureSuccess(
        TransactionalBatchResponse response,
        StreamVersion expectedVersion,
        StreamMetadata metadata)
    {
        if (response.IsSuccessStatusCode)
        {
            return metadata;
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            throw new StreamConflictException(
                metadata.StreamId,
                metadata.Version,
                metadata.State,
                expectedVersion,
                expectedState: null,
                $"Conflict on writing to stream. Status Code: {response.StatusCode}.");
        }

        throw new CosmosException(
            response.ErrorMessage,
            response.StatusCode,
            subStatusCode: 0,
            response.ActivityId,
            response.RequestCharge);
    }
}
