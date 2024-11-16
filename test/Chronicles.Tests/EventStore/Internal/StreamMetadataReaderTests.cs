using System.Net;
using Chronicles.Documents;
using Chronicles.EventStore;
using Chronicles.EventStore.Internal;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Chronicles.Tests.EventStore.Internal;

public class StreamMetadataReaderTests
{
    [Theory, AutoNSubstituteData]
    internal async Task GetAsync_Should_Return_StreamMetadata(
        [Frozen] IDocumentReader<StreamMetadataDocument> reader,
        StreamMetadataReader sut,
        StreamId streamId,
        StreamMetadataDocument document,
        CancellationToken cancellationToken)
    {
        reader
            .ReadAsync<StreamMetadataDocument>("meta-data", (string)streamId, options: null, storeName: null, cancellationToken)
            .Returns(document);

        var result = await sut.GetAsync(
            streamId,
            storeName: null,
            cancellationToken);

        result
            .Should()
            .BeEquivalentTo(document);
    }

    [Theory, AutoNSubstituteData]
    internal async Task GetAsync_Should_Return_New_StreamMetadata_When_NotFound(
        [Frozen] IDocumentReader<StreamMetadataDocument> reader,
        [Frozen(Matching.DirectBaseType)] FakeTimeProvider dateTimeProvider,
        StreamId streamId,
        StreamMetadataReader sut,
        CancellationToken cancellationToken)
    {
        dateTimeProvider.AutoAdvanceAmount = TimeSpan.Zero;
        reader
            .ReadAsync<StreamMetadataDocument>("meta-data", (string)streamId, options: null, storeName: null, cancellationToken)
            .ThrowsAsync(new CosmosException("not found", HttpStatusCode.NotFound, 404, "not found", 0));

        var result = await sut.GetAsync(
            streamId,
            storeName: null,
            cancellationToken);

        result
            .Should()
            .BeEquivalentTo(
                new StreamMetadataDocument(
                    Id: JsonPropertyNames.StreamMetadataId,
                    Pk: (string)streamId,
                    streamId,
                    StreamState.New,
                    Version: 0,
                    dateTimeProvider.GetUtcNow()));
    }

    [Theory, AutoNSubstituteData]
    internal async Task GetAsync_Should_(
        [Frozen] IDocumentReader<StreamMetadataDocument> reader,
        [Frozen(Matching.DirectBaseType)] FakeTimeProvider dateTimeProvider,
        StreamId streamId,
        StreamMetadataReader sut,
        CancellationToken cancellationToken)
    {
        dateTimeProvider.AutoAdvanceAmount = TimeSpan.Zero;
        reader
            .ReadAsync<StreamMetadataDocument>("meta-data", (string)streamId, options: null, storeName: null, cancellationToken)
            .ThrowsAsync(new CosmosException("not found", HttpStatusCode.NotFound, 404, "not found", 0));

        var result = await sut.GetAsync(
            streamId,
            storeName: null,
            cancellationToken);

        result
            .Should()
            .BeEquivalentTo(
                new StreamMetadataDocument(
                    Id: JsonPropertyNames.StreamMetadataId,
                    Pk: (string)streamId,
                    streamId,
                    StreamState.New,
                    Version: 0,
                    dateTimeProvider.GetUtcNow()));
    }
}