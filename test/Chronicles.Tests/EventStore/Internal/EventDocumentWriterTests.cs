using System.Net;
using Chronicles.Documents;
using Chronicles.EventStore;
using Chronicles.EventStore.Internal;
using Microsoft.Azure.Cosmos;
using NSubstitute;

namespace Chronicles.Tests.EventStore.Internal;

public class EventDocumentWriterTests
{
    internal sealed record SampleEvent(string Name);

    internal sealed class TxResponse : TransactionalBatchResponse
    {
        public StreamMetadataDocument? ResultReturned { get; set; }

        public HttpStatusCode StatusCodeReturned { get; set; } = HttpStatusCode.OK;

        public override bool IsSuccessStatusCode => StatusCode == HttpStatusCode.OK;

        public override HttpStatusCode StatusCode => StatusCodeReturned;

        public override TransactionalBatchOperationResult<T> GetOperationResultAtIndex<T>(int index)
            => new OperationResult<T>(ResultReturned);
    }

    internal sealed class OperationResult<T>(object? resource)
        : TransactionalBatchOperationResult<T>
    {
        public override T Resource { get; set; } = (T)resource!;
    }

    [Theory, AutoNSubstituteData]
    internal async Task WriteStreamAsync_Should_Produce_Events(
        [Frozen] IDocumentWriter<EventDocumentBase> writer,
        [Frozen] IEventDocumentBatchProducer batchProducer,
        IDocumentTransaction<EventDocumentBase> transaction,
        EventMetadata eventMetadata,
        SampleEvent eventData,
        TxResponse batchResponse,
        EventDocumentWriter sut,
        CancellationToken cancellationToken)
    {
        var streamMetadata = new StreamMetadataDocument(
            "id",
            "pk",
            new StreamId("stream", "1"),
            StreamState.Active,
            1,
            DateTimeOffset.Now);
        batchProducer
            .FromEvents(default, default, default, default)
            .ReturnsForAnyArgs(
                new EventDocumentBatch(
                    streamMetadata,
                    [new EventDocument("id", "pk", eventMetadata, eventData)]));

        batchResponse.ResultReturned = streamMetadata;
        batchResponse.StatusCodeReturned = HttpStatusCode.OK;

        transaction
            .CommitAsync(default)
            .ReturnsForAnyArgs(batchResponse);

        writer
            .CreateTransaction(default, default)
            .ReturnsForAnyArgs(transaction);

        await sut.WriteStreamAsync(
            streamMetadata,
            [eventData],
            options: null,
            storeName: null,
            cancellationToken);

        _ = batchProducer
            .Received(1)
            .FromEvents(
                Arg.Is<IReadOnlyList<object>>(x => x.Count == 1),
                Arg.Is<StreamMetadataDocument>(x => x.StreamId == streamMetadata.StreamId),
                Arg.Any<string>(),
                Arg.Any<StreamWriteOptions?>());
    }

    [Theory]
    [InlineAutoNSubstituteData(null)]
    [InlineAutoNSubstituteData("storeName")]
    internal async Task WriteStreamAsync_Should_Create_Transaction(
        string? storeName,
        [Frozen] IDocumentWriter<EventDocumentBase> writer,
        [Frozen] IEventDocumentBatchProducer batchProducer,
        IDocumentTransaction<EventDocumentBase> transaction,
        EventMetadata eventMetadata,
        SampleEvent eventData,
        TxResponse batchResponse,
        EventDocumentWriter sut,
        CancellationToken cancellationToken)
    {
        var streamMetadata = new StreamMetadataDocument(
            "id",
            "pk",
            new StreamId("stream", "1"),
            StreamState.Active,
            1,
            DateTimeOffset.Now);
        batchProducer
            .FromEvents(default, default, default, default)
            .ReturnsForAnyArgs(
                new EventDocumentBatch(
                    streamMetadata,
                    [new EventDocument("id", "pk", eventMetadata, eventData)]));

        batchResponse.ResultReturned = streamMetadata;
        batchResponse.StatusCodeReturned = HttpStatusCode.OK;

        transaction
            .CommitAsync(default)
            .ReturnsForAnyArgs(batchResponse);

        writer
            .CreateTransaction(default, default)
            .ReturnsForAnyArgs(transaction);

        await sut.WriteStreamAsync(
            streamMetadata,
            [eventData],
            options: null,
            storeName: storeName,
            cancellationToken);

        _ = writer
            .Received(1)
            .CreateTransaction(
                partitionKey: "stream.1",
                storeName: storeName);
    }

    [Theory, AutoNSubstituteData]
    internal async Task WriteStreamAsync_Should_Write_MetaData_To_Transaction(
        [Frozen] IDocumentWriter<EventDocumentBase> writer,
        [Frozen] IEventDocumentBatchProducer batchProducer,
        IDocumentTransaction<EventDocumentBase> transaction,
        EventMetadata eventMetadata,
        SampleEvent eventData,
        TxResponse batchResponse,
        EventDocumentWriter sut,
        CancellationToken cancellationToken)
    {
        var streamMetadata = new StreamMetadataDocument(
            "id",
            "pk",
            new StreamId("stream", "1"),
            StreamState.Active,
            1,
            DateTimeOffset.Now);
        var batch = new EventDocumentBatch(
            streamMetadata,
            [new EventDocument("id", "pk", eventMetadata, eventData)]);

        batchProducer
            .FromEvents(default, default, default, default)
            .ReturnsForAnyArgs(batch);

        batchResponse.ResultReturned = streamMetadata;
        batchResponse.StatusCodeReturned = HttpStatusCode.OK;

        transaction
            .CommitAsync(default)
            .ReturnsForAnyArgs(batchResponse);

        writer
            .CreateTransaction(default, default)
            .ReturnsForAnyArgs(transaction);

        await sut.WriteStreamAsync(
            streamMetadata,
            [eventData],
            options: null,
            storeName: null,
            cancellationToken);

        // Must return metadata with content response
        _ = transaction
            .Received(1)
            .Write(
                document: batch.Metadata,
                options: Arg.Is<TransactionalBatchItemRequestOptions?>(o => o!.EnableContentResponseOnWrite == false));
    }

    [Theory, AutoNSubstituteData]
    internal async Task WriteStreamAsync_Should_Create_Event_To_Transaction(
        [Frozen] IDocumentWriter<EventDocumentBase> writer,
        [Frozen] IEventDocumentBatchProducer batchProducer,
        IDocumentTransaction<EventDocumentBase> transaction,
        EventMetadata eventMetadata,
        SampleEvent eventData,
        TxResponse batchResponse,
        EventDocumentWriter sut,
        CancellationToken cancellationToken)
    {
        var streamMetadata = new StreamMetadataDocument(
            "id",
            "pk",
            new StreamId("stream", "1"),
            StreamState.Active,
            1,
            DateTimeOffset.Now);
        var batch = new EventDocumentBatch(
            streamMetadata,
            [new EventDocument("id", "pk", eventMetadata, eventData)]);

        batchProducer
            .FromEvents(default, default, default, default)
            .ReturnsForAnyArgs(batch);

        batchResponse.ResultReturned = streamMetadata;
        batchResponse.StatusCodeReturned = HttpStatusCode.OK;

        transaction
            .CommitAsync(default)
            .ReturnsForAnyArgs(batchResponse);

        writer
            .CreateTransaction(default, default)
            .ReturnsForAnyArgs(transaction);

        await sut.WriteStreamAsync(
            streamMetadata,
            [eventData],
            options: null,
            storeName: null,
            cancellationToken);

        _ = transaction
            .Received(1)
            .Create(
                document: batch.Events.First(),
                options: Arg.Is<TransactionalBatchItemRequestOptions?>(o => o!.EnableContentResponseOnWrite == false));
    }

    [Theory, AutoNSubstituteData]
    internal async Task WriteStreamAsync_Should_Commit_Transaction(
        [Frozen] IDocumentWriter<EventDocumentBase> writer,
        [Frozen] IEventDocumentBatchProducer batchProducer,
        IDocumentTransaction<EventDocumentBase> transaction,
        EventMetadata eventMetadata,
        SampleEvent eventData,
        TxResponse batchResponse,
        EventDocumentWriter sut,
        CancellationToken cancellationToken)
    {
        var streamMetadata = new StreamMetadataDocument(
            "id",
            "pk",
            new StreamId("stream", "1"),
            StreamState.Active,
            1,
            DateTimeOffset.Now);
        var batch = new EventDocumentBatch(
            streamMetadata,
            [new EventDocument("id", "pk", eventMetadata, eventData)]);

        batchProducer
            .FromEvents(default, default, default, default)
            .ReturnsForAnyArgs(batch);

        batchResponse.ResultReturned = streamMetadata;
        batchResponse.StatusCodeReturned = HttpStatusCode.OK;

        transaction
            .CommitAsync(default)
            .ReturnsForAnyArgs(batchResponse);

        writer
            .CreateTransaction(default, default)
            .ReturnsForAnyArgs(transaction);

        await sut.WriteStreamAsync(
            streamMetadata,
            [eventData],
            options: null,
            storeName: null,
            cancellationToken);

        _ = transaction
            .Received(1)
            .CommitAsync(cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    internal async Task WriteStreamAsync_Should_Throw_CosmosException_On_TooManyRequest(
        [Frozen] IDocumentWriter<EventDocumentBase> writer,
        [Frozen] IEventDocumentBatchProducer batchProducer,
        IDocumentTransaction<EventDocumentBase> transaction,
        EventMetadata eventMetadata,
        SampleEvent eventData,
        TxResponse batchResponse,
        EventDocumentWriter sut,
        CancellationToken cancellationToken)
    {
        var streamMetadata = new StreamMetadataDocument(
            "id",
            "pk",
            new StreamId("stream", "1"),
            StreamState.Active,
            1,
            DateTimeOffset.Now);
        var batch = new EventDocumentBatch(
            streamMetadata,
            [new EventDocument("id", "pk", eventMetadata, eventData)]);

        batchProducer
            .FromEvents(default, default, default, default)
            .ReturnsForAnyArgs(batch);

        batchResponse.ResultReturned = streamMetadata;
        batchResponse.StatusCodeReturned = HttpStatusCode.TooManyRequests;

        transaction
            .CommitAsync(default)
            .ReturnsForAnyArgs(batchResponse);

        writer
            .CreateTransaction(default, default)
            .ReturnsForAnyArgs(transaction);

        await FluentActions
            .Awaiting(() => sut.WriteStreamAsync(
                streamMetadata,
                [eventData],
                options: null,
                storeName: null,
                cancellationToken))
            .Should().ThrowAsync<CosmosException>();
    }

    [Theory, AutoNSubstituteData]
    internal async Task WriteStreamAsync_Should_Throw_StreamConflictException_When_Unsuccessful(
        [Frozen] IDocumentWriter<EventDocumentBase> writer,
        [Frozen] IEventDocumentBatchProducer batchProducer,
        IDocumentTransaction<EventDocumentBase> transaction,
        EventMetadata eventMetadata,
        SampleEvent eventData,
        TxResponse batchResponse,
        EventDocumentWriter sut,
        CancellationToken cancellationToken)
    {
        var streamMetadata = new StreamMetadataDocument(
            "id",
            "pk",
            new StreamId("stream", "1"),
            StreamState.Active,
            1,
            DateTimeOffset.Now);
        var batch = new EventDocumentBatch(
            streamMetadata,
            [new EventDocument("id", "pk", eventMetadata, eventData)]);

        batchProducer
            .FromEvents(default, default, default, default)
            .ReturnsForAnyArgs(batch);

        batchResponse.ResultReturned = streamMetadata;
        batchResponse.StatusCodeReturned = HttpStatusCode.Conflict;

        transaction
            .CommitAsync(default)
            .ReturnsForAnyArgs(batchResponse);

        writer
            .CreateTransaction(default, default)
            .ReturnsForAnyArgs(transaction);

        await FluentActions
            .Awaiting(() => sut.WriteStreamAsync(
                streamMetadata,
                [eventData],
                options: null,
                storeName: null,
                cancellationToken))
            .Should().ThrowAsync<StreamConflictException>();
    }
}
