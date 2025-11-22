using Chronicles.EventStore;
using Chronicles.EventStore.Internal;
using NSubstitute;

namespace Chronicles.Tests.EventStore.Internal;

public class EventStreamReaderTests
{
    [Theory, AutoNSubstituteData]
    internal async Task GetCheckpointAsync_Should_Read_Checkpoint(
        [Frozen] ICheckpointReader reader,
        StreamId streamId,
        string checkpointName,
        Checkpoint<TestCheckpoint> checkpoint,
        EventStreamReader sut,
        CancellationToken cancellationToken)
    {
        reader
            .ReadAsync<TestCheckpoint>(
                checkpointName,
                streamId,
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(checkpoint);

        var cp = await sut.GetCheckpointAsync<TestCheckpoint>(
            checkpointName,
            streamId,
            cancellationToken: cancellationToken);

        cp.Should().Be(checkpoint);
    }

    [Theory, AutoNSubstituteData]
    internal async Task GetMetadataAsync_Should_Read_Metadata(
        [Frozen] IStreamMetadataReader reader,
        StreamId streamId,
        StreamMetadataDocument document,
        EventStreamReader sut,
        CancellationToken cancellationToken)
    {
        reader
            .GetAsync(
                streamId,
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(document);

        var metadata = await sut.GetMetadataAsync(
            streamId,
            cancellationToken: cancellationToken);

        metadata.Should().Be(document);
    }

    [Theory, AutoNSubstituteData]
    internal void QueryStreamsAsync_Should_Query_Metadata(
        [Frozen] IStreamMetadataReader reader,
        DateTimeOffset createdAfter,
        string storeName,
        EventStreamReader sut,
        CancellationToken cancellationToken)
    {
        _ = sut.QueryStreamsAsync(
            "filter",
            createdAfter,
            storeName,
            cancellationToken);

        _ = reader
            .Received(1)
            .QueryAsync(
                "filter",
                createdAfter,
                storeName,
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    internal async Task ReadAsync_Should_Read_Metadata_When_Not_Provide_In_Options(
        [Frozen] IStreamMetadataReader reader,
        StreamId streamId,
        StreamMetadataDocument document,
        EventStreamReader sut,
        CancellationToken cancellationToken)
    {
        reader
            .GetAsync(
                streamId,
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(document);

        await foreach (var evt in sut.ReadAsync(
            streamId,
            cancellationToken: cancellationToken))
        {
        }

        _ = reader
            .Received(1)
            .GetAsync(
                streamId,
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());
    }

    [Theory, AutoNSubstituteData]
    internal async Task ReadAsync_Should_Not_Read_Metadata_When_Provided_In_Options(
        [Frozen] IStreamMetadataReader reader,
        StreamId streamId,
        StreamMetadataDocument document,
        EventStreamReader sut,
        CancellationToken cancellationToken)
    {
        var options = new StreamReadOptions
        {
            Metadata = document
        };

        await foreach (var evt in sut.ReadAsync(streamId, options, cancellationToken: cancellationToken))
        {
        }

        _ = reader
            .DidNotReceive()
            .GetAsync(
                streamId,
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());
    }

    [Theory, AutoNSubstituteData]
    internal async Task ReadAsync_Should_Iterate_Document_Reader(
        [Frozen] IEventDocumentReader eventDocumentReader,
        StreamId streamId,
        StreamMetadataDocument document,
        StreamEvent[] events,
        EventStreamReader sut,
        CancellationToken cancellationToken)
    {
        var options = new StreamReadOptions
        {
            Metadata = document
        };

        eventDocumentReader
            .ReadAsync(
                options.Metadata,
                options,
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(events.ToAsyncEnumerable());

        var eventsRead = new List<StreamEvent>();
        await foreach (var evt in sut.ReadAsync(streamId, options, cancellationToken: cancellationToken))
        {
            eventsRead.Add(evt);
        }

        eventsRead
            .Should()
            .BeEquivalentTo(events);
    }
}
