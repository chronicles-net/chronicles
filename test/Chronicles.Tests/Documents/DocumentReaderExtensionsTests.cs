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
        string storeName,
        CancellationToken cancellationToken)
    {
        await reader.FindAsync<TestDocument, TestDocument>(
                documentId,
                partitionKey,
                options,
                storeName,
                cancellationToken);

        _ = reader
            .Received(1)
            .ReadAsync<TestDocument>(
                documentId,
                partitionKey,
                options,
                storeName,
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public async Task FindAsync_Returns_TestDocument_From_CosmosReader(
        IDocumentReader<TestDocument> reader,
        string partitionKey,
        string documentId,
        ItemRequestOptions options,
        string storeName,
        TestDocument document,
        CancellationToken cancellationToken)
    {
        reader
            .ReadAsync<TestDocument>(default, default, default, default, default)
            .ReturnsForAnyArgs(document);
        var result = await reader.FindAsync<TestDocument, TestDocument>(
                documentId,
                partitionKey,
                options,
                storeName,
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
        string storeName,
        CancellationToken cancellationToken)
    {
        exception = new(
            exception.Message,
            System.Net.HttpStatusCode.NotFound,
            exception.SubStatusCode,
            exception.ActivityId,
            exception.RequestCharge);
        reader
            .ReadAsync<TestDocument>(default, default, default, default, default)
            .ReturnsForAnyArgs(Task.FromException<TestDocument>(exception));

        var response = await reader.FindAsync<TestDocument, TestDocument>(
            documentId,
            partitionKey,
            options,
            storeName,
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
        string storeName,
        CancellationToken cancellationToken)
    {
        await reader.FindAsync(
            documentId,
            partitionKey,
            options,
            storeName,
            cancellationToken);

        _ = reader
            .Received(1)
            .ReadAsync<TestDocument>(
                documentId,
                partitionKey,
                options,
                storeName,
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
                storeName: null,
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public async Task ReadAsync_Has_Overload_With_Default_TResult(
        IDocumentReader<TestDocument> reader,
        string documentId,
        string partitionKey,
        ItemRequestOptions options,
        string storeName,
        CancellationToken cancellationToken)
    {
        await reader.ReadAsync(
            documentId,
            partitionKey,
            options,
            storeName,
            cancellationToken);

        _ = reader
            .Received(1)
            .ReadAsync<TestDocument>(
                documentId,
                partitionKey,
                options,
                storeName,
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
                storeName: null,
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public void ReadAllAsync_Calls_CreateQuery_On_CosmosReader(
        IDocumentReader<TestDocument> reader,
        string partitionKey,
        QueryRequestOptions options,
        string storeName,
        IQueryable<TestDocument> queryable,
        CancellationToken cancellationToken)
    {
        _ = reader.ReadAllAsync(
            partitionKey,
            options,
            storeName,
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
        string storeName,
        QueryDefinition query,
        CancellationToken cancellationToken)
    {
        reader
            .CreateQuery<TestDocument>(default, default)
            .ReturnsForAnyArgs(query);

        _ = reader.ReadAllAsync(
            partitionKey,
            options,
            storeName,
            cancellationToken);

        _ = reader
            .Received(1)
            .QueryAsync<TestDocument>(
                query,
                partitionKey,
                options,
                storeName,
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public void ReadAllAsync_Returns_From_QueryAsync(
        IAsyncEnumerable<TestDocument> queryResult,
        IDocumentReader<TestDocument> reader,
        string partitionKey,
        QueryRequestOptions options,
        string storeName,
        CancellationToken cancellationToken)
    {
        reader
            .QueryAsync<TestDocument>(default, default, default, default, default)
            .ReturnsForAnyArgs(queryResult);

        var result = reader.ReadAllAsync(
            partitionKey,
            options,
            storeName,
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
            .CreateQuery<TestDocument>(default, default)
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
                storeName: null,
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
                storeName: null,
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
                maxItemCount,
                continuationToken,
                options: null,
                storeName: null,
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public void QueryAsync_Has_Overload_With_QueryExpression(
        IDocumentReader<TestDocument> reader,
        QueryDefinition query,
        QueryExpression<TestDocument, TestAggregate> queryExpression,
        string? partitionKey,
        QueryRequestOptions options,
        string storeName,
        CancellationToken cancellationToken)
    {
        reader
            .CreateQuery(queryExpression)
            .ReturnsForAnyArgs(query);

        _ = reader.QueryAsync(
            queryExpression,
            partitionKey,
            options,
            storeName,
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
                storeName,
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public void PagedQueryAsync_Has_Overload_With_QueryExpression(
        IDocumentReader<TestDocument> reader,
        QueryDefinition query,
        QueryExpression<TestDocument, TestAggregate> queryExpression,
        string? partitionKey,
        int maxItemCount,
        string continuationToken,
        QueryRequestOptions options,
        string storeName,
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
            options,
            storeName,
            cancellationToken);

        _ = reader
            .Received(1)
            .CreateQuery(queryExpression);

        _ = reader
            .Received(1)
            .PagedQueryAsync<TestAggregate>(
                query,
                partitionKey,
                maxItemCount,
                continuationToken,
                options,
                storeName,
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
                storeName: null,
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
                maxItemCount,
                continuationToken,
                options: null,
                storeName: null,
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public void QueryAsync_Has_Overload_With_Predicate(
        IDocumentReader<TestDocument> reader,
        QueryDefinition query,
        Expression<Func<TestDocument, bool>> queryPredicate,
        string? partitionKey,
        QueryRequestOptions options,
        string storeName,
        CancellationToken cancellationToken)
    {
        reader
            .CreateQuery<TestDocument>(default)
            .ReturnsForAnyArgs(query);

        _ = reader.QueryAsync(
            queryPredicate,
            partitionKey,
            options,
            storeName,
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
                storeName,
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public void PagedQueryAsync_Has_Overload_With_Predicate(
        IDocumentReader<TestDocument> reader,
        QueryDefinition query,
        Expression<Func<TestDocument, bool>> queryPredicate,
        string? partitionKey,
        int maxItemCount,
        string continuationToken,
        QueryRequestOptions options,
        string storeName,
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
            options,
            storeName,
            cancellationToken);

        _ = reader
            .Received(1)
            .CreateQuery(Arg.Any<QueryExpression<TestDocument, TestDocument>>());

        _ = reader
            .Received(1)
            .PagedQueryAsync<TestDocument>(
                query,
                partitionKey,
                maxItemCount,
                continuationToken,
                options,
                storeName,
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
                storeName: null,
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
                maxItemCount,
                continuationToken,
                options: null,
                storeName: null,
                cancellationToken);
    }
}
