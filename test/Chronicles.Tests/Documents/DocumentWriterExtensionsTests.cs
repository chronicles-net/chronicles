using System.Net;
using Chronicles.Documents;
using Microsoft.Azure.Cosmos;
using NSubstitute;

namespace Chronicles.Tests.Documents;

public class DocumentWriterExtensionsTests
{
    [Theory, AutoNSubstituteData]
    public async Task Should_Return_True_When_Trying_To_Delete_Existing_Resource(
        IDocumentWriter<TestDocument> writer,
        string documentId,
        string documentPk,
        ItemRequestOptions options,
        CancellationToken cancellationToken)
    {
        var deleted = await writer.TryDeleteAsync(
            documentId,
            documentPk,
            options,
            cancellationToken);

        deleted
            .Should()
            .BeTrue();

        _ = writer
            .Received(1)
            .DeleteAsync(
                documentId,
                documentPk,
                options,
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public async Task Should_Return_False_When_Trying_To_Delete_NonExisting_Resource(
        IDocumentWriter<TestDocument> writer,
        string documentId,
        string documentPk,
        ItemRequestOptions options,
        CancellationToken cancellationToken)
    {
        writer
            .WhenForAnyArgs(w => w.DeleteAsync(default, default, default, default))
            .Do(c => throw new CosmosException("fake", HttpStatusCode.NotFound, 0, "1", 1));

        var deleted = await writer.TryDeleteAsync(
            documentId,
            documentPk,
            options,
            cancellationToken);

        deleted
            .Should()
            .BeFalse();

        _ = writer
            .Received(1)
            .DeleteAsync(
                documentId,
                documentPk,
                options,
                cancellationToken);
    }
}
