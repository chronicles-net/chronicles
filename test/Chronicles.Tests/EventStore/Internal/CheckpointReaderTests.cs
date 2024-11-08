using Chronicles.Documents;
using Chronicles.EventStore;
using Chronicles.EventStore.Internal;
using NSubstitute;

namespace Chronicles.Tests.EventStore.Internal;

public class CheckpointReaderTests
{
    public record CheckpointData(string Name);

    [Theory, AutoNSubstituteData]
    internal async Task FindAsync_Should_ReadAsync_On_Document(
        [Frozen] IDocumentReader<Checkpoint> reader,
        string name,
        StreamId streamId,
        string? storeName,
        CheckpointReader sut,
        CancellationToken cancellationToken)
    {
        await sut.ReadAsync<CheckpointData>(
            name,
            streamId,
            storeName,
            cancellationToken);

        _ = reader
            .Received(1)
            .ReadAsync<CheckpointDocument<CheckpointData>>(
                name,
                (string)streamId,
                options: null,
                storeName: storeName,
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    internal async Task FindAsync_Should_Return_Checkpoint_Data(
        [Frozen] IDocumentReader<Checkpoint> reader,
        string name,
        StreamId streamId,
        string? storeName,
        CheckpointDocument<CheckpointData> checkpointData,
        CheckpointReader sut,
        CancellationToken cancellationToken)
    {
        reader
            .ReadAsync<CheckpointDocument<CheckpointData>>(
                name,
                (string)streamId,
                options: null,
                storeName: storeName,
                cancellationToken)
            .Returns(checkpointData);

        var checkpoint = await sut.ReadAsync<CheckpointData>(
            name,
            streamId,
            storeName,
            cancellationToken);

        checkpoint.Should().Be(checkpointData);
    }
}