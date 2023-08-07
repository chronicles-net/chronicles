using System.Linq.Expressions;
using Chronicles.Documents;
using Microsoft.Azure.Cosmos;
using NSubstitute;

namespace Chronicles.Tests.Documents;

public class DocumentReaderExtensionsTests
{
    [Theory, AutoNSubstituteData]
    public async Task FindAsync_Calls_ReadAsync_On_CosmosReader(
        IDocumentReader<TestDocument> reader,
        string documentId,
        string partitionKey,
        ItemRequestOptions options,
        CancellationToken cancellationToken)
    {
        await reader.FindAsync<TestDocument, TestDocument>(
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
        var result = await reader.FindAsync<TestDocument, TestDocument>(
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

        var response = await reader.FindAsync<TestDocument, TestDocument>(
            documentId,
            partitionKey,
            options,
            cancellationToken);

        response
            .Should()
            .BeNull();
    }

    [Theory, AutoNSubstituteData]
    public async Task FindAsync_Has_Overload_With_Default_TResult(
        IDocumentReader<TestDocument> reader,
        string documentId,
        string partitionKey,
        ItemRequestOptions options,
        CancellationToken cancellationToken)
    {
        await reader.FindAsync(
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
    public async Task FindAsync_Has_Overload_With_Default_TResult_And_Options(
        IDocumentReader<TestDocument> reader,
        string documentId,
        string partitionKey,
        CancellationToken cancellationToken)
    {
        await reader.FindAsync(
            documentId,
            partitionKey,
            cancellationToken);

        _ = reader
            .Received(1)
            .ReadAsync<TestDocument>(
                documentId,
                partitionKey,
                options: null,
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public async Task ReadAsync_Has_Overload_With_Default_TResult(
        IDocumentReader<TestDocument> reader,
        string documentId,
        string partitionKey,
        ItemRequestOptions options,
        CancellationToken cancellationToken)
    {
        await reader.ReadAsync(
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
    public async Task ReadAsync_Has_Overload_With_Default_TResult_And_Options(
        IDocumentReader<TestDocument> reader,
        string documentId,
        string partitionKey,
        CancellationToken cancellationToken)
    {
        await reader.ReadAsync(
            documentId,
            partitionKey,
            cancellationToken);

        _ = reader
            .Received(1)
            .ReadAsync<TestDocument>(
                documentId,
                partitionKey,
                options: null,
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public void ReadAllAsync_Calls_CreateQuery_On_CosmosReader(
        IDocumentReader<TestDocument> reader,
        string partitionKey,
        QueryRequestOptions options,
        IQueryable<TestDocument> queryable,
        CancellationToken cancellationToken)
    {
        _ = reader.ReadAllAsync(
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

        _ = reader.ReadAllAsync(
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

        var result = reader.ReadAllAsync(
            partitionKey,
            options,
            cancellationToken);

        result
            .Should()
            .Be(queryResult);
    }

    [Theory, AutoNSubstituteData]
    public void ReadAllAsync_Has_Overload_With_Default_Options(
        IDocumentReader<TestDocument> reader,
        string partitionKey,
        QueryDefinition query,
        CancellationToken cancellationToken)
    {
        reader
            .CreateQuery<TestDocument>(default)
            .ReturnsForAnyArgs(query);

        _ = reader.ReadAllAsync(
            partitionKey,
            cancellationToken);

        _ = reader
            .Received(1)
            .QueryAsync<TestDocument>(
                query,
                partitionKey,
                options: null,
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public void QueryAsync_Has_Overload_With_Default_Options(
        IDocumentReader<TestDocument> reader,
        QueryDefinition query,
        string? partitionKey,
        CancellationToken cancellationToken)
    {
        _ = reader.QueryAsync(
            query,
            partitionKey,
            cancellationToken);

        _ = reader
            .Received(1)
            .QueryAsync<TestDocument>(
                query,
                partitionKey,
                options: null,
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public void PagedQueryAsync_Has_Overload_With_Default_Options(
        IDocumentReader<TestDocument> reader,
        QueryDefinition query,
        string? partitionKey,
        int maxItemCount,
        string continuationToken,
        CancellationToken cancellationToken)
    {
        _ = reader.PagedQueryAsync(
            query,
            partitionKey,
            maxItemCount,
            continuationToken,
            cancellationToken);

        _ = reader
            .Received(1)
            .PagedQueryAsync<TestDocument>(
                query,
                partitionKey,
                options: null,
                maxItemCount,
                continuationToken,
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public void QueryAsync_Has_Overload_With_QueryExpression(
        IDocumentReader<TestDocument> reader,
        QueryDefinition query,
        QueryExpression<TestDocument, TestAggregate> queryExpression,
        string? partitionKey,
        QueryRequestOptions options,
        CancellationToken cancellationToken)
    {
        reader
            .CreateQuery(queryExpression)
            .ReturnsForAnyArgs(query);

        _ = reader.QueryAsync(
            queryExpression,
            partitionKey,
            options,
            cancellationToken);

        _ = reader
            .Received(1)
            .CreateQuery(queryExpression);

        _ = reader
            .Received(1)
            .QueryAsync<TestAggregate>(
                query,
                partitionKey,
                options,
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public void PagedQueryAsync_Has_Overload_With_QueryExpression(
        IDocumentReader<TestDocument> reader,
        QueryDefinition query,
        QueryExpression<TestDocument, TestAggregate> queryExpression,
        string? partitionKey,
        QueryRequestOptions options,
        int maxItemCount,
        string continuationToken,
        CancellationToken cancellationToken)
    {
        reader
            .CreateQuery(queryExpression)
            .ReturnsForAnyArgs(query);

        _ = reader.PagedQueryAsync(
            queryExpression,
            partitionKey,
            options,
            maxItemCount,
            continuationToken,
            cancellationToken);

        _ = reader
            .Received(1)
            .CreateQuery(queryExpression);

        _ = reader
            .Received(1)
            .PagedQueryAsync<TestAggregate>(
                query,
                partitionKey,
                options,
                maxItemCount,
                continuationToken,
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public void QueryAsync_Has_Overload_With_QueryExpression_And_Default_Options(
        IDocumentReader<TestDocument> reader,
        QueryDefinition query,
        QueryExpression<TestDocument, TestAggregate> queryExpression,
        string? partitionKey,
        CancellationToken cancellationToken)
    {
        reader
            .CreateQuery(queryExpression)
            .ReturnsForAnyArgs(query);

        _ = reader.QueryAsync(
            queryExpression,
            partitionKey,
            cancellationToken);

        _ = reader
            .Received(1)
            .CreateQuery(queryExpression);

        _ = reader
            .Received(1)
            .QueryAsync<TestAggregate>(
                query,
                partitionKey,
                options: null,
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public void PagedQueryAsync_Has_Overload_With_QueryExpression_And_Default_Options(
        IDocumentReader<TestDocument> reader,
        QueryDefinition query,
        QueryExpression<TestDocument, TestAggregate> queryExpression,
        string? partitionKey,
        int maxItemCount,
        string continuationToken,
        CancellationToken cancellationToken)
    {
        reader
            .CreateQuery(queryExpression)
            .ReturnsForAnyArgs(query);

        _ = reader.PagedQueryAsync(
            queryExpression,
            partitionKey,
            maxItemCount,
            continuationToken,
            cancellationToken);

        _ = reader
            .Received(1)
            .CreateQuery(queryExpression);

        _ = reader
            .Received(1)
            .PagedQueryAsync<TestAggregate>(
                query,
                partitionKey,
                options: null,
                maxItemCount,
                continuationToken,
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public void QueryAsync_Has_Overload_With_Predicate(
        IDocumentReader<TestDocument> reader,
        QueryDefinition query,
        Expression<Func<TestDocument, bool>> queryPredicate,
        string? partitionKey,
        QueryRequestOptions options,
        CancellationToken cancellationToken)
    {
        reader
            .CreateQuery<TestDocument>(default)
            .ReturnsForAnyArgs(query);

        _ = reader.QueryAsync(
            queryPredicate,
            partitionKey,
            options,
            cancellationToken);

        _ = reader
            .Received(1)
            .CreateQuery(Arg.Any<QueryExpression<TestDocument, TestDocument>>());

        _ = reader
            .Received(1)
            .QueryAsync<TestDocument>(
                query,
                partitionKey,
                options,
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public void PagedQueryAsync_Has_Overload_With_Predicate(
        IDocumentReader<TestDocument> reader,
        QueryDefinition query,
        Expression<Func<TestDocument, bool>> queryPredicate,
        string? partitionKey,
        QueryRequestOptions options,
        int maxItemCount,
        string continuationToken,
        CancellationToken cancellationToken)
    {
        reader
            .CreateQuery<TestDocument>(default)
            .ReturnsForAnyArgs(query);

        _ = reader.PagedQueryAsync(
            queryPredicate,
            partitionKey,
            options,
            maxItemCount,
            continuationToken,
            cancellationToken);

        _ = reader
            .Received(1)
            .CreateQuery(Arg.Any<QueryExpression<TestDocument, TestDocument>>());

        _ = reader
            .Received(1)
            .PagedQueryAsync<TestDocument>(
                query,
                partitionKey,
                options,
                maxItemCount,
                continuationToken,
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public void QueryAsync_Has_Overload_With_Predicate_And_Default_Options(
        IDocumentReader<TestDocument> reader,
        QueryDefinition query,
        Expression<Func<TestDocument, bool>> queryPredicate,
        string? partitionKey,
        CancellationToken cancellationToken)
    {
        reader
            .CreateQuery<TestDocument>(default)
            .ReturnsForAnyArgs(query);

        _ = reader.QueryAsync(
            queryPredicate,
            partitionKey,
            cancellationToken);

        _ = reader
            .Received(1)
            .CreateQuery(Arg.Any<QueryExpression<TestDocument, TestDocument>>());

        _ = reader
            .Received(1)
            .QueryAsync<TestDocument>(
                query,
                partitionKey,
                options: null,
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public void PagedQueryAsync_Has_Overload_With_Predicate_And_Default_Options(
        IDocumentReader<TestDocument> reader,
        QueryDefinition query,
        Expression<Func<TestDocument, bool>> queryPredicate,
        string? partitionKey,
        int maxItemCount,
        string continuationToken,
        CancellationToken cancellationToken)
    {
        reader
            .CreateQuery<TestDocument>(default)
            .ReturnsForAnyArgs(query);

        _ = reader.PagedQueryAsync(
            queryPredicate,
            partitionKey,
            maxItemCount,
            continuationToken,
            cancellationToken);

        _ = reader
            .Received(1)
            .CreateQuery(Arg.Any<QueryExpression<TestDocument, TestDocument>>());

        _ = reader
            .Received(1)
            .PagedQueryAsync<TestDocument>(
                query,
                partitionKey,
                options: null,
                maxItemCount,
                continuationToken,
                cancellationToken);
    }
}
