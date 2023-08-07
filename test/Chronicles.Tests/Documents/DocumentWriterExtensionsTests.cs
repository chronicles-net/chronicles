using System.Net;
using AutoFixture.AutoNSubstitute;
using Chronicles.Documents;
using Microsoft.Azure.Cosmos;
using NSubstitute;

namespace Chronicles.Tests.Documents;

public class DocumentWriterExtensionsTests
{
    [Theory, AutoNSubstituteData]
    public async Task CreateAsync_Has_Overload_With_Default_Options(
        IDocumentWriter<TestDocument> writer,
        TestDocument document,
        CancellationToken cancellationToken)
    {
        await writer.CreateAsync(
            document,
            cancellationToken);

        _ = writer
            .Received(1)
            .CreateAsync(
                document,
                options: null,
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public async Task WriteAsync_Has_Overload_With_Default_Options(
        IDocumentWriter<TestDocument> writer,
        TestDocument document,
        CancellationToken cancellationToken)
    {
        await writer.WriteAsync(
            document,
            cancellationToken);

        _ = writer
            .Received(1)
            .WriteAsync(
                document,
                options: null,
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public async Task ReplaceAsync_Has_Overload_With_Default_Options(
        IDocumentWriter<TestDocument> writer,
        TestDocument document,
        CancellationToken cancellationToken)
    {
        await writer.ReplaceAsync(
            document,
            cancellationToken);

        _ = writer
            .Received(1)
            .ReplaceAsync(
                document,
                options: null,
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public async Task DeleteAsync_Has_Overload_With_Default_Options(
        IDocumentWriter<TestDocument> writer,
        string documentId,
        string partitionKey,
        CancellationToken cancellationToken)
    {
        await writer.DeleteAsync(
            documentId,
            partitionKey,
            cancellationToken);

        _ = writer
            .Received(1)
            .DeleteAsync(
                documentId,
                partitionKey,
                options: null,
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public async Task TryDelete_Should_Return_True_If_Document_Exists(
        IDocumentWriter<TestDocument> writer,
        string documentId,
        string partitionKey,
        ItemRequestOptions options,
        CancellationToken cancellationToken)
    {
        var deleted = await writer.TryDeleteAsync(
            documentId,
            partitionKey,
            options,
            cancellationToken);

        deleted
            .Should()
            .BeTrue();

        _ = writer
            .Received(1)
            .DeleteAsync(
                documentId,
                partitionKey,
                options,
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public async Task TryDelete_Should_Return_False_Of_Document_Does_Not_Exist(
        IDocumentWriter<TestDocument> writer,
        string documentId,
        string partitionKey,
        ItemRequestOptions options,
        CancellationToken cancellationToken)
    {
        writer
            .WhenForAnyArgs(w => w.DeleteAsync(default, default, default, default))
            .Do(c => throw new CosmosException("fake", HttpStatusCode.NotFound, 0, "1", 1));

        var deleted = await writer.TryDeleteAsync(
            documentId,
            partitionKey,
            options,
            cancellationToken);

        deleted
            .Should()
            .BeFalse();

        _ = writer
            .Received(1)
            .DeleteAsync(
                documentId,
                partitionKey,
                options,
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public async Task TryDeleteAsync_Has_Overload_With_Default_Options(
        IDocumentWriter<TestDocument> writer,
        string documentId,
        string partitionKey,
        CancellationToken cancellationToken)
    {
        await writer.TryDeleteAsync(
            documentId,
            partitionKey,
            cancellationToken);

        _ = writer
            .Received(1)
            .DeleteAsync(
                documentId,
                partitionKey,
                options: null,
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public async Task UpdateAsync_Has_Overload_With_Action(
        IDocumentWriter<TestDocument> writer,
        TestDocument document,
        [Substitute] Action<TestDocument> updateDocument,
        int retries,
        CancellationToken cancellationToken)
    {
        await writer.UpdateAsync(
            document.Id,
            document.Pk,
            updateDocument,
            retries,
            cancellationToken);

        _ = writer
            .Received(1)
            .UpdateAsync(
                document.Id,
                document.Pk,
                Arg.Any<Func<TestDocument, Task>>(),
                retries,
                cancellationToken);

        _ = writer
            .ReceivedCallWithArgument<Func<TestDocument, Task>>()
            .Invoke(document);
        updateDocument
            .Received(1)
            .Invoke(document);
    }

    [Theory, AutoNSubstituteData]
    public async Task UpdateOrCreateAsync_Has_Overload_With_Action(
        IDocumentWriter<TestDocument> writer,
        TestDocument document,
        [Substitute] Action<TestDocument> updateDocument,
        int retries,
        CancellationToken cancellationToken)
    {
        var getDefaultDocument = () => document;
        await writer.UpdateOrCreateAsync(
            getDefaultDocument,
            updateDocument,
            retries,
            cancellationToken);

        _ = writer
            .Received(1)
            .UpdateOrCreateAsync(
                getDefaultDocument,
                Arg.Any<Func<TestDocument, Task>>(),
                retries,
                cancellationToken);

        _ = writer
            .ReceivedCallWithArgument<Func<TestDocument, Task>>()
            .Invoke(document);
        updateDocument
            .Received(1)
            .Invoke(document);
    }
}
