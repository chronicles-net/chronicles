using Chronicles.Documents;
using Chronicles.EventStore;
using Chronicles.EventStore.Internal;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Chronicles.Tests.EventStore.Internal;

public class CheckpointWriterTests
{
    public record CheckpointData(string Name);

    [Theory, AutoNSubstituteData]
    internal async Task WriteAsync_Should_WriteAsync_On_Document(
        [Frozen(Matching.DirectBaseType)] FakeTimeProvider dateTimeProvider,
        [Frozen] IDocumentWriter<CheckpointDocument<object?>> writer,
        string name,
        StreamId streamId,
        StreamVersion version,
        object? state,
        string? storeName,
        CheckpointWriter sut,
        CancellationToken cancellationToken)
    {
        await sut.WriteAsync(
            name,
            streamId,
            version,
            state,
            storeName,
            cancellationToken);

        _ = writer
            .Received(1)
            .WriteAsync(
                document: Arg.Is<CheckpointDocument<object?>>(cp
                    => cp.Id == name
                    && cp.Pk == (string)streamId
                    && cp.Name == name
                    && cp.StreamId == streamId
                    && cp.StreamVersion == version
                    && cp.Timestamp == dateTimeProvider.Start
                    && cp.State == state),
                options: null,
                storeName: storeName,
                cancellationToken);
    }
}