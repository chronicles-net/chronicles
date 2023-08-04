using Chronicles.Documents;
using Microsoft.Azure.Cosmos;
using NSubstitute;

namespace Chronicles.Tests.Cosmos;

public class CosmosReaderExtensionsTests
{
    [Theory, AutoNSubstituteData]
    public async Task FindAsync_Calls_ReadAsync_On_CosmosReader(
        IDocumentReader<TestDocument> reader,
        string documentId,
        string partitionKey,
        ItemRequestOptions options,
        CancellationToken cancellationToken)
    {
        await DocumentReaderExtensions
            .FindAsync<TestDocument, TestDocument>(
                reader,
                documentId,
                partitionKey,
                options,
                cancellationToken);

        _ = reader
            .Received(1)
            .ReadAsync<TestDocument>(
                documentId,
                partitionKey,
                options,
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public async Task FindAsync_Returns_TestDocument_From_CosmosReader(
        IDocumentReader<TestDocument> reader,
        string partitionKey,
        string documentId,
        ItemRequestOptions options,
        TestDocument document,
        CancellationToken cancellationToken)
    {
        reader
            .ReadAsync<TestDocument>(default, default, default, default)
            .ReturnsForAnyArgs(document);
        var result = await DocumentReaderExtensions
            .FindAsync<TestDocument, TestDocument>(
                reader,
                documentId,
                partitionKey,
                options,
                cancellationToken);
        result
            .Should()
            .Be(document);
    }

    [Theory, AutoNSubstituteData]
    public async Task FindAsync_Returns_Default_When_CosmosReader_Throws_NotFound(
        IDocumentReader<TestDocument> reader,
        CosmosException exception,
        string documentId,
        string partitionKey,
        ItemRequestOptions options,
        CancellationToken cancellationToken)
    {
        exception = new(
            exception.Message,
            System.Net.HttpStatusCode.NotFound,
            exception.SubStatusCode,
            exception.ActivityId,
            exception.RequestCharge);
        reader
            .ReadAsync<TestDocument>(default, default, default, default)
            .ReturnsForAnyArgs(Task.FromException<TestDocument>(exception));

        var response = await DocumentReaderExtensions.FindAsync<TestDocument, TestDocument>(
            reader,
            documentId,
            partitionKey,
            options,
            cancellationToken);

        response
            .Should()
            .BeNull();
    }

    [Theory, AutoNSubstituteData]
    public void ReadAllAsync_Calls_CreateQuery_On_CosmosReader(
        IDocumentReader<TestDocument> reader,
        string partitionKey,
        QueryRequestOptions options,
        IQueryable<TestDocument> queryable,
        CancellationToken cancellationToken)
    {
        _ = DocumentReaderExtensions.ReadAllAsync(
            reader,
            partitionKey,
            options,
            cancellationToken);

        _ = reader
            .Received(1)
            .CreateQuery(Arg.Any<QueryExpression<TestDocument, TestDocument>>());

        reader
            .ReceivedCallWithArgument<QueryExpression<TestDocument, TestDocument>>()
            .Invoke(queryable)
            .Should()
            .BeSameAs(queryable);
    }

    [Theory, AutoNSubstituteData]
    public void ReadAllAsync_Calls_QueyrAsync_On_Cosmos(
        IDocumentReader<TestDocument> reader,
        string partitionKey,
        QueryRequestOptions options,
        QueryDefinition query,
        CancellationToken cancellationToken)
    {
        reader
            .CreateQuery<TestDocument>(default)
            .ReturnsForAnyArgs(query);

        _ = DocumentReaderExtensions.ReadAllAsync(
            reader,
            partitionKey,
            options,
            cancellationToken);

        _ = reader
            .Received(1)
            .QueryAsync<TestDocument>(
                query,
                partitionKey,
                options,
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public void ReadAllAsync_Returns_From_QueryAsync(
        IAsyncEnumerable<TestDocument> queryResult,
        IDocumentReader<TestDocument> reader,
        string partitionKey,
        QueryRequestOptions options,
        CancellationToken cancellationToken)
    {
        reader
            .QueryAsync<TestDocument>(default, default, default, default)
            .ReturnsForAnyArgs(queryResult);

        var result = DocumentReaderExtensions.ReadAllAsync(
            reader,
            partitionKey,
            options,
            cancellationToken);

        result
            .Should()
            .Be(queryResult);
    }
}
