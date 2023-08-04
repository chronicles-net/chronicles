using AutoFixture;
using Chronicles.Documents;
using Chronicles.Documents.Internal;
using Dasync.Collections;
using Microsoft.Azure.Cosmos;
using NSubstitute;

namespace Chronicles.Tests.Cosmos.Internal;

public class CosmosReaderTests
{
    private readonly IQueryable<TestDocument> queryable;
    private readonly QueryDefinition query;
    private readonly ItemResponse<TestDocument> itemResponse;
    private readonly FeedIterator<TestDocument> feedIterator;
    private readonly FeedResponse<TestDocument> feedResponse;
    private readonly TestDocument document;
    private readonly Container container;
    private readonly ICosmosContainerProvider containerProvider;
    private readonly ICosmosLinqQuery linqQuery;
    private readonly CosmosReader<TestDocument> sut;

    public CosmosReaderTests()
    {
        var fixture = FixtureFactory.Create();

        query = fixture.Create<QueryDefinition>();
        document = fixture.Create<TestDocument>();
        queryable = new[] { document }.AsQueryable();

        itemResponse = Substitute.For<ItemResponse<TestDocument>>();
        itemResponse
            .Resource
            .Returns(document);

        feedResponse = Substitute.For<FeedResponse<TestDocument>>();
        feedIterator = Substitute.For<FeedIterator<TestDocument>>();
        feedIterator
            .ReadNextAsync(default)
            .ReturnsForAnyArgs(feedResponse);

        container = Substitute.For<Container>();
        container
            .ReadItemAsync<TestDocument>(default, default, default, default)
            .ReturnsForAnyArgs(itemResponse);

        container
            .GetItemQueryIterator<TestDocument>(default(QueryDefinition), default, default)
            .ReturnsForAnyArgs(feedIterator);

        container
            .GetItemQueryIterator<TestDocument>(default(string), default, default)
            .ReturnsForAnyArgs(feedIterator);

        container
            .GetItemLinqQueryable<TestDocument>(default, default, default, default)
            .ReturnsForAnyArgs(c => queryable);

        containerProvider = Substitute.For<ICosmosContainerProvider>();
        containerProvider
            .GetContainer<TestDocument>()
            .Returns(container, null);

        linqQuery = Substitute.For<ICosmosLinqQuery>();
        linqQuery
            .GetQueryDefinition(Arg.Any<IQueryable<TestDocument>>())
            .ReturnsForAnyArgs(query);
        linqQuery
            .GetFeedIterator(Arg.Any<IQueryable<TestDocument>>())
            .ReturnsForAnyArgs(feedIterator);

        sut = new CosmosReader<TestDocument>(containerProvider, linqQuery);
    }

    [Fact]
    public void Implements_Interface()
        => sut.Should().BeAssignableTo<IDocumentReader<TestDocument>>();

    [Theory, AutoNSubstituteData]
    public async Task ReadAsync_Uses_The_Right_Container(
        string documentId,
        string partitionKey,
        ItemRequestOptions options,
        CancellationToken cancellationToken)
    {
        await sut.ReadAsync<TestDocument>(
            documentId,
            partitionKey,
            options,
            cancellationToken);

        containerProvider
            .Received(1)
            .GetContainer<TestDocument>();
    }

    [Theory, AutoNSubstituteData]
    public async Task ReadAsync_Reads_Item_In_Container(
        string documentId,
        string partitionKey,
        ItemRequestOptions options,
        CancellationToken cancellationToken)
    {
        await sut.ReadAsync<TestDocument>(
            documentId,
            partitionKey,
            options,
            cancellationToken);

        _ = container
            .Received(1)
            .ReadItemAsync<TestDocument>(
                documentId,
                new PartitionKey(partitionKey),
                options,
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public async Task ReadAsync_Returns_Item_Read_From_Container(
        string documentId,
        string partitionKey,
        ItemRequestOptions options,
        CancellationToken cancellationToken)
    {
        var result = await sut.ReadAsync<TestDocument>(
            documentId,
            partitionKey,
            options,
            cancellationToken);
        result
            .Should()
            .Be(itemResponse.Resource);
    }

    [Theory, AutoNSubstituteData]
    public void ReadAsync_Throws_Expection_When_TestDocument_IsNot_Found(
        CosmosException exception,
        string documentId,
        string partitionKey,
        ItemRequestOptions options,
        CancellationToken cancellationToken)
    {
        container
            .ReadItemAsync<TestDocument>(default, default, default, default)
            .Returns(Task.FromException<ItemResponse<TestDocument>>(exception));

        FluentActions
            .Awaiting(() => sut.ReadAsync<TestDocument>(documentId, partitionKey, options, cancellationToken))
            .Should()
            .ThrowAsync<CosmosException>();
    }

    [Theory, AutoNSubstituteData]
    public void QueryAsync_Uses_The_Right_Container(
        QueryDefinition query,
        string partitionKey,
        QueryRequestOptions options,
        CancellationToken cancellationToken)
    {
        _ = sut.QueryAsync<TestDocument>(
            query,
            partitionKey,
            options,
            cancellationToken);

        containerProvider
            .Received(1)
            .GetContainer<TestDocument>();
    }

    [Theory, AutoNSubstituteData]
    public async Task QueryAsync_Returns_Empty_No_More_Result(
        QueryDefinition query,
        string partitionKey,
        QueryRequestOptions options,
        CancellationToken cancellationToken)
    {
        feedIterator.HasMoreResults.Returns(false);

        var response = await sut.QueryAsync<TestDocument>(
            query,
            partitionKey,
            options,
            cancellationToken)
            .ToListAsync(cancellationToken);

        _ = feedIterator
            .Received(1)
            .HasMoreResults;

        _ = feedIterator
            .Received(0)
            .ReadNextAsync(default);

        response
            .Should()
            .BeEmpty();
    }

    [Theory, AutoNSubstituteData]
    public async Task QueryAsync_Returns_Empty_When_Query_Matches_Non(
        QueryDefinition query,
        string partitionKey,
        QueryRequestOptions options,
        CancellationToken cancellationToken)
    {
        feedIterator.HasMoreResults.Returns(true, false);

        var response = await sut.QueryAsync<TestDocument>(
            query,
            partitionKey,
            options,
            cancellationToken)
            .ToListAsync(cancellationToken);

        _ = feedIterator
            .Received(2)
            .HasMoreResults;

        _ = feedIterator
            .Received(1)
            .ReadNextAsync(default);

        response
            .Should()
            .BeEmpty();
    }

    [Theory, AutoNSubstituteData]
    public async Task QueryAsync_Returns_Items_When_Query_Matches(
        QueryDefinition query,
        string partitionKey,
        QueryRequestOptions options,
        CancellationToken cancellationToken)
    {
        feedIterator
            .HasMoreResults
            .Returns(true, false);

        feedResponse
            .GetEnumerator()
            .Returns(new List<TestDocument> { document }.GetEnumerator());

        var response = await sut.QueryAsync<TestDocument>(
            query,
            partitionKey,
            options,
            cancellationToken)
            .ToListAsync(cancellationToken);

        _ = feedIterator
            .Received(2)
            .HasMoreResults;

        _ = feedIterator
            .Received(1)
            .ReadNextAsync(default);

        response
            .Should()
            .NotBeEmpty();

        response[0]
            .Should()
            .Be(document);
    }

    [Theory, AutoNSubstituteData]
    public void PagedQueryAsync_Uses_The_Right_Container(
        QueryDefinition query,
        string partitionKey,
        QueryRequestOptions options,
        int maxItemCount,
        string continuationToken,
        CancellationToken cancellationToken)
    {
        _ = sut.PagedQueryAsync<TestDocument>(
            query,
            partitionKey,
            options,
            maxItemCount,
            continuationToken,
            cancellationToken);

        containerProvider
            .Received(1)
            .GetContainer<TestDocument>();
    }

    [Theory, AutoNSubstituteData]
    public void PagedQueryAsync_Gets_ItemQueryIterator(
        QueryDefinition query,
        string partitionKey,
        QueryRequestOptions options,
        int maxItemCount,
        string continuationToken,
        CancellationToken cancellationToken)
    {
        _ = sut.PagedQueryAsync<TestDocument>(
            query,
            partitionKey,
            options,
            maxItemCount,
            continuationToken,
            cancellationToken);

        container
            .Received(1)
            .GetItemQueryIterator<TestDocument>(
                query,
                continuationToken,
                requestOptions: Arg.Is<QueryRequestOptions>(o
                    => o.PartitionKey == new PartitionKey(partitionKey)
                    && o.MaxItemCount == maxItemCount));
    }

    [Theory, AutoNSubstituteData]
    public async Task PagedQueryAsync_Returns_Empty_When_No_More_Result(
        QueryDefinition query,
        string partitionKey,
        QueryRequestOptions options,
        int maxItemCount,
        string continuationToken,
        CancellationToken cancellationToken)
    {
        feedIterator.HasMoreResults.Returns(false);

        var response = await sut
            .PagedQueryAsync<TestDocument>(
                query,
                partitionKey,
                options,
                maxItemCount,
                continuationToken,
                cancellationToken);

        _ = feedIterator
            .Received(1)
            .HasMoreResults;

        _ = feedIterator
            .Received(0)
            .ReadNextAsync(default);

        response.Items
            .Should()
            .BeEmpty();
        response.ContinuationToken
            .Should()
            .BeNull();
    }

    [Theory, AutoNSubstituteData]
    public async Task PagedQueryAsync_Returns_Items_When_Query_Matches(
        QueryDefinition query,
        string partitionKey,
        QueryRequestOptions options,
        int maxItemCount,
        string continuationToken,
        List<TestDocument> records,
        CancellationToken cancellationToken)
    {
        feedIterator
            .HasMoreResults
            .Returns(true);
        feedResponse
            .ContinuationToken
            .Returns(continuationToken);
        feedResponse
            .GetEnumerator()
            .Returns(records.GetEnumerator());

        var response = await sut
            .PagedQueryAsync<TestDocument>(
                query,
                partitionKey,
                options,
                maxItemCount,
                null,
                cancellationToken);

        _ = feedIterator
            .Received(1)
            .HasMoreResults;

        _ = feedIterator
            .Received(1)
            .ReadNextAsync(default);

        response.Items
            .Should()
            .BeEquivalentTo(records);

        response.ContinuationToken
            .Should()
            .Be(continuationToken);
    }

    [Theory, AutoNSubstituteData]
    public void Multiple_Operations_Uses_Same_Container(
        QueryDefinition query,
        string documentId,
        string partitionKey,
        int maxItemCount,
        string continuationToken,
        CancellationToken cancellationToken)
    {
        _ = sut.ReadAsync<TestDocument>(documentId, partitionKey, null, cancellationToken);
        _ = sut.ReadAsync<TestDocumentSubClass>(documentId, partitionKey, null, cancellationToken);
        _ = sut.QueryAsync<TestDocument>(query, partitionKey, null, cancellationToken).ToListAsync(cancellationToken);
        _ = sut.QueryAsync<TestAggregate>(query, partitionKey, null, cancellationToken).ToListAsync(cancellationToken);
        _ = sut.PagedQueryAsync<TestDocument>(query, partitionKey, null, maxItemCount, continuationToken, cancellationToken);
        _ = sut.PagedQueryAsync<TestAggregate>(query, partitionKey, null, maxItemCount, continuationToken, cancellationToken);

        containerProvider
            .Received()
            .GetContainer<TestDocument>();

        container
            .ReceivedCalls()
            .Should()
            .HaveCount(6);
    }

    [Theory, AutoNSubstituteData]
    public void CreateQuery_Creates_QueryDefinition_From_Linq_Query(
        QueryExpression<TestDocument, TestDocument> query,
        IQueryable<TestDocument> queryResult)
    {
        query
            .Invoke(Arg.Any<IQueryable<TestDocument>>())
            .Returns(queryResult);

        var result = sut.CreateQuery(query);

        query
            .Received(1)
            .Invoke(queryable);

        linqQuery
            .Received(1)
            .GetQueryDefinition(queryResult);
    }
}
