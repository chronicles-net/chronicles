using System.Collections.Immutable;
using Chronicles.EventStore;
using Chronicles.EventStore.Internal;
using NSubstitute;

namespace Chronicles.Tests.EventStore.Internal;

public class EventStreamWriterTests
{
    [Theory, AutoNSubstituteData]
    internal async Task WriteAsync_With_Empty_Events_Does_Not_Write(
        [Frozen] IEventDocumentWriter writer,
        StreamId streamId,
        EventStreamWriter sut)
    {
        await sut.WriteAsync(
            streamId,
            []);

        _ = writer
            .DidNotReceive()
            .WriteStreamAsync(
                Arg.Any<StreamMetadata>(),
                Arg.Any<IImmutableList<object>>(),
                Arg.Any<StreamWriteOptions>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());
    }

    [Theory, AutoNSubstituteData]
    internal async Task WriteAsync_With_Empty_Metadata_Options_Should_Read_Metadata(
        [Frozen] IStreamMetadataReader reader,
        StreamId streamId,
        EventStreamWriter sut)
    {
        await sut.WriteAsync(
            streamId,
            [new { Name = "name" }],
            new StreamWriteOptions());

        _ = reader
            .Received()
            .GetAsync(
                streamId,
                storeName: Arg.Any<string>(),
                Arg.Any<CancellationToken>());
    }

    [Theory, AutoNSubstituteData]
    internal async Task WriteAsync_With_Metadata_Options_ShouldNot_Read_Metadata(
        [Frozen] IStreamMetadataReader reader,
        StreamId streamId,
        StreamMetadata metadata,
        EventStreamWriter sut)
    {
        await sut.WriteAsync(
            streamId,
            [new { Name = "name" }],
            new StreamWriteOptions
            {
                Metadata = metadata,
            });

        _ = reader
            .DidNotReceive()
            .GetAsync(
                Arg.Any<StreamId>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());
    }

    [Theory, AutoNSubstituteData]
    internal async Task WriteAsync_With_No_Required_StreamVersion_Should_Retry_5_Time_On_Conflict(
        [Frozen] IEventDocumentWriter writer,
        [Frozen] IStreamMetadataReader reader,
        StreamId streamId,
        EventStreamWriter sut)
    {
        writer
            .WriteStreamAsync(
                Arg.Any<StreamMetadata>(),
                Arg.Any<IImmutableList<object>>(),
                Arg.Any<StreamWriteOptions>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(
                Task.FromException<StreamWriteResult>(
                    new StreamConflictException(streamId, 0, StreamState.New, null, null, "conflict")));

        await FluentActions
            .Awaiting(
                () => sut.WriteAsync(
                    streamId,
                    [new { Name = "name" }]))
            .Should()
            .ThrowAsync<StreamConflictException>();

        _ = writer
            .Received(5)
            .WriteStreamAsync(
                Arg.Any<StreamMetadata>(),
                Arg.Any<IImmutableList<object>>(),
                Arg.Any<StreamWriteOptions>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());

        _ = reader
            .Received(5)
            .GetAsync(
                streamId,
                storeName: Arg.Any<string>(),
                Arg.Any<CancellationToken>());
    }

    [Theory, AutoNSubstituteData]
    internal async Task SetCheckpointAsync_Should_Read_Metadata(
        [Frozen] IStreamMetadataReader reader,
        StreamId streamId,
        StreamMetadataDocument metadata,
        string storeName,
        string checkpointName,
        EventStreamWriter sut)
    {
        metadata = metadata with
        {
            StreamId = streamId,
            State = StreamState.Active,
            Version = 10,
        };

        reader
            .GetAsync(
                streamId,
                storeName,
                Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(metadata);

        await sut.SetCheckpointAsync(
            checkpointName,
            streamId,
            5,
            storeName: storeName);

        _ = reader
            .Received(1)
            .GetAsync(
                streamId,
                storeName: storeName,
                Arg.Any<CancellationToken>());
    }

    [Theory, AutoNSubstituteData]
    internal async Task SetCheckpointAsync_With_No_State_Should_Write_Without_State(
        [Frozen] IStreamMetadataReader reader,
        [Frozen] ICheckpointWriter checkpointWriter,
        StreamId streamId,
        StreamMetadataDocument metadata,
        string storeName,
        string checkpointName,
        EventStreamWriter sut)
    {
        metadata = metadata with
        {
            StreamId = streamId,
            State = StreamState.Active,
            Version = 10,
        };

        reader
            .GetAsync(
                streamId,
                storeName,
                Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(metadata);

        await sut.SetCheckpointAsync(
            checkpointName,
            streamId,
            5,
            storeName: storeName);

        _ = checkpointWriter
            .Received()
            .WriteAsync(
                checkpointName,
                streamId,
                5,
                state: null,
                storeName: storeName,
                Arg.Any<CancellationToken>());
    }

    [Theory, AutoNSubstituteData]
    internal async Task SetCheckpointAsync_With_State_Should_Write_State(
        [Frozen] IStreamMetadataReader reader,
        [Frozen] ICheckpointWriter checkpointWriter,
        StreamId streamId,
        StreamMetadataDocument metadata,
        string storeName,
        string checkpointName,
        EventStreamWriter sut)
    {
        var state = new { Name = "name" };
        metadata = metadata with
        {
            StreamId = streamId,
            State = StreamState.Active,
            Version = 10,
        };

        reader
            .GetAsync(
                streamId,
                storeName,
                Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(metadata);

        await sut.SetCheckpointAsync(
            checkpointName,
            streamId,
            5,
            state: state,
            storeName: storeName);

        _ = checkpointWriter
            .Received()
            .WriteAsync(
                checkpointName,
                streamId,
                5,
                state: state,
                storeName: storeName,
                Arg.Any<CancellationToken>());
    }

    [Theory, AutoNSubstituteData]
    internal async Task SetCheckpointAsync_With_Version_Outside_Of_Stream_Should_Throw_Conflict(
        [Frozen] IStreamMetadataReader reader,
        [Frozen] ICheckpointWriter checkpointWriter,
        StreamId streamId,
        StreamMetadataDocument metadata,
        string storeName,
        string checkpointName,
        EventStreamWriter sut)
    {
        var state = new { Name = "name" };
        metadata = metadata with
        {
            StreamId = streamId,
            State = StreamState.New,
            Version = 0,
        };

        reader
            .GetAsync(
                streamId,
                storeName,
                Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(metadata);

        await FluentActions
            .Awaiting(
                () => sut.SetCheckpointAsync(
                    checkpointName,
                    streamId,
                    5,
                    state: state,
                    storeName: storeName))
            .Should()
            .ThrowAsync<StreamConflictException>();

        _ = checkpointWriter
            .DidNotReceive()
            .WriteAsync(
                Arg.Any<string>(),
                Arg.Any<StreamId>(),
                Arg.Any<StreamVersion>(),
                Arg.Any<object>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());
    }
}