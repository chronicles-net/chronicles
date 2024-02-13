using System.Net;
using Chronicles.Documents;
using Chronicles.EventStore.Internal.Events;
using Microsoft.Azure.Cosmos;

namespace Chronicles.EventStore.Internal.Streams;

internal class StreamEventWriter
{
    private readonly IDocumentWriter<StreamDocument> writer;
    private readonly StreamMetadataReader metadataReader;
    private readonly EventDocumentBatchProducer batchProducer;

    public StreamEventWriter(
        IDocumentWriter<StreamDocument> writer,
        StreamMetadataReader metadataReader,
        EventDocumentBatchProducer batchProducer)
    {
        this.writer = writer;
        this.metadataReader = metadataReader;
        this.batchProducer = batchProducer;
    }

    public virtual async Task<StreamMetadata> WriteAsync(
        StreamId streamId,
        IReadOnlyCollection<object> events,
        StreamVersion version,
        StreamWriteOptions? options,
        string? storeName,
        CancellationToken cancellationToken)
    {
        var metadata = await metadataReader
            .GetAsync(
                streamId,
                storeName: storeName,
                cancellationToken);

        if (!metadata.Version.IsValid(version))
        {
            throw new StreamConflictException(
                metadata.StreamId,
                metadata.Version,
                version,
                $"Stream is not at the required version.");
        }

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
            .CommitAsync(cancellationToken);

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
            expectedVersion,
            "Optimistic concurrency conflict on writing to stream.");
    }
}