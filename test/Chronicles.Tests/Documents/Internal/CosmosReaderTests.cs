using AutoFixture;
using Chronicles.Documents;
using Chronicles.Documents.Internal;
using Dasync.Collections;
using Microsoft.Azure.Cosmos;
using NSubstitute;

namespace Chronicles.Tests.Documents.Internal;

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
            .GetContainer<TestDocument>(default)
            .ReturnsForAnyArgs(container);

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
        string storeName,
        CancellationToken cancellationToken)
    {
        await sut.ReadAsync<TestDocument>(
            documentId,
            partitionKey,
            options,
            storeName,
            cancellationToken);

        containerProvider
            .Received(1)
            .GetContainer<TestDocument>(storeName);
    }

    [Theory, AutoNSubstituteData]
    public async Task ReadAsync_Reads_Item_In_Container(
        string documentId,
        string partitionKey,
        ItemRequestOptions options,
        string storeName,
        CancellationToken cancellationToken)
    {
        await sut.ReadAsync<TestDocument>(
            documentId,
            partitionKey,
            options,
            storeName,
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
        string storeName,
        CancellationToken cancellationToken)
    {
        var result = await sut.ReadAsync<TestDocument>(
            documentId,
            partitionKey,
            options,
            storeName,
            cancellationToken);
        result
            .Should()
            .Be(itemResponse.Resource);
    }

    [Theory, AutoNSubstituteData]
    public void ReadAsync_Throws_Exception_When_TestDocument_IsNot_Found(
        CosmosException exception,
        string documentId,
        string partitionKey,
        ItemRequestOptions options,
        string storeName,
        CancellationToken cancellationToken)
    {
        container
            .ReadItemAsync<TestDocument>(default, default, default, default)
            .Returns(Task.FromException<ItemResponse<TestDocument>>(exception));

        FluentActions
            .Awaiting(() => sut.ReadAsync<TestDocument>(documentId, partitionKey, options, storeName, cancellationToken))
            .Should()
            .ThrowAsync<CosmosException>();
    }

    [Theory, AutoNSubstituteData]
    public void QueryAsync_Uses_The_Right_Container(
        QueryDefinition query,
        string partitionKey,
        QueryRequestOptions options,
        string storeName,
        CancellationToken cancellationToken)
    {
        _ = sut.QueryAsync<TestDocument>(
            query,
            partitionKey,
            options,
            storeName,
            cancellationToken);

        containerProvider
            .Received(1)
            .GetContainer<TestDocument>(storeName);
    }

    [Theory, AutoNSubstituteData]
    public void QueryAsync_Calls_GetItemQuery_Iterator_On_Container(
        QueryDefinition query,
        QueryRequestOptions options,
        string storeName,
        CancellationToken cancellationToken)
    {
        _ = sut.QueryAsync<TestDocument>(
            query,
            partitionKey: null,
            options,
            storeName,
            cancellationToken);

        containerProvider
            .Received(1)
            .GetContainer<TestDocument>(storeName);
        container
            .Received(1)
            .GetItemQueryIterator<TestDocument>(
                query,
                null,
                options);
    }

    [Theory, AutoNSubstituteData]
    public void QueryAsync_Calls_GetItemQuery_Iterator_On_Container_With_PartitionKey(
        QueryDefinition query,
        string partitionKey,
        QueryRequestOptions options,
        string storeName,
        CancellationToken cancellationToken)
    {
        _ = sut.QueryAsync<TestDocument>(
            query,
            partitionKey,
            options,
            storeName,
            cancellationToken);

        containerProvider
            .Received(1)
            .GetContainer<TestDocument>(storeName);
        container
            .Received(1)
            .GetItemQueryIterator<TestDocument>(
                query,
                null,
                Arg.Is<QueryRequestOptions>(o => o.PartitionKey == new PartitionKey(partitionKey)));
        container
            .ReceivedCallWithArgument<QueryRequestOptions>()
            .Should()
            .BeEquivalentTo(options, c => c.Excluding(o => o.PartitionKey));
    }

    [Theory, AutoNSubstituteData]
    public async Task QueryAsync_Returns_Empty_No_More_Result(
        QueryDefinition query,
        string partitionKey,
        QueryRequestOptions options,
        string storeName,
        CancellationToken cancellationToken)
    {
        feedIterator.HasMoreResults.Returns(false);

        var response = await sut.QueryAsync<TestDocument>(
            query,
            partitionKey,
            options,
            storeName,
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
        string storeName,
        CancellationToken cancellationToken)
    {
        feedIterator.HasMoreResults.Returns(true, false);

        var response = await sut.QueryAsync<TestDocument>(
            query,
            partitionKey,
            options,
            storeName,
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
        string storeName,
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
            storeName,
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
        int maxItemCount,
        string continuationToken,
        QueryRequestOptions options,
        string storeName,
        CancellationToken cancellationToken)
    {
        _ = sut.PagedQueryAsync<TestDocument>(
            query,
            partitionKey,
            maxItemCount,
            continuationToken,
            options,
            storeName,
            cancellationToken);

        containerProvider
            .Received(1)
            .GetContainer<TestDocument>(storeName);
    }

    [Theory, AutoNSubstituteData]
    public async Task PagedQueryAsync_Calls_GetItemQuery_Iterator_On_Container(
        QueryDefinition query,
        string continuationToken,
        QueryRequestOptions options,
        string storeName,
        CancellationToken cancellationToken)
    {
        await sut.PagedQueryAsync<TestDocument>(
            query,
            partitionKey: null,
            maxItemCount: null,
            continuationToken,
            options,
            storeName,
            cancellationToken);

        containerProvider
            .Received(1)
            .GetContainer<TestDocument>(storeName);
        container
            .Received(1)
            .GetItemQueryIterator<TestDocument>(
                query,
                continuationToken,
                options);
    }

    [Theory, AutoNSubstituteData]
    public async Task PagedQueryAsync_Calls_GetItemQuery_Iterator_On_Container_With_PartitionKey(
        QueryDefinition query,
        string partitionKey,
        int maxItemCount,
        string continuationToken,
        QueryRequestOptions options,
        string storeName,
        CancellationToken cancellationToken)
    {
        await sut.PagedQueryAsync<TestDocument>(
            query,
            partitionKey,
            maxItemCount,
            continuationToken,
            options,
            storeName,
            cancellationToken);

        containerProvider
            .Received(1)
            .GetContainer<TestDocument>(storeName);
        container
            .Received(1)
            .GetItemQueryIterator<TestDocument>(
                query,
                continuationToken,
                Arg.Is<QueryRequestOptions>(o
                    => o.PartitionKey == new PartitionKey(partitionKey)
                    && o.MaxItemCount == maxItemCount));
        container
            .ReceivedCallWithArgument<QueryRequestOptions>()
            .Should()
            .BeEquivalentTo(options, c => c
                .Excluding(o => o.PartitionKey)
                .Excluding(o => o.MaxItemCount));
    }

    [Theory, AutoNSubstituteData]
    public void PagedQueryAsync_Gets_ItemQueryIterator(
        QueryDefinition query,
        string partitionKey,
        int maxItemCount,
        string continuationToken,
        QueryRequestOptions options,
        string storeName,
        CancellationToken cancellationToken)
    {
        _ = sut.PagedQueryAsync<TestDocument>(
            query,
            partitionKey,
            maxItemCount,
            continuationToken,
            options,
            storeName,
            cancellationToken);

        containerProvider
            .Received(1)
            .GetContainer<TestDocument>(storeName);
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
        int maxItemCount,
        string continuationToken,
        QueryRequestOptions options,
        string storeName,
        CancellationToken cancellationToken)
    {
        feedIterator.HasMoreResults.Returns(false);

        var response = await sut
            .PagedQueryAsync<TestDocument>(
                query,
                partitionKey,
                maxItemCount,
                continuationToken,
                options,
                storeName,
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
        int maxItemCount,
        string continuationToken,
        QueryRequestOptions options,
        string storeName,
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
                maxItemCount,
                continuationToken: null,
                options,
                storeName,
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
    public void Each_Operation_Gets_The_Container(
        QueryDefinition query,
        string documentId,
        string partitionKey,
        int maxItemCount,
        string continuationToken,
        string storeName,
        CancellationToken cancellationToken)
    {
        _ = sut.ReadAsync<TestDocument>(documentId, partitionKey, options: null, storeName, cancellationToken);
        _ = sut.ReadAsync<TestDocumentSubClass>(documentId, partitionKey, options: null, storeName, cancellationToken);
        _ = sut.QueryAsync<TestDocument>(query, partitionKey, options: null, storeName, cancellationToken).ToListAsync(cancellationToken);
        _ = sut.QueryAsync<TestAggregate>(query, partitionKey, options: null, storeName, cancellationToken).ToListAsync(cancellationToken);
        _ = sut.PagedQueryAsync<TestDocument>(query, partitionKey, maxItemCount, continuationToken, options: null, storeName, cancellationToken);
        _ = sut.PagedQueryAsync<TestAggregate>(query, partitionKey, maxItemCount, continuationToken, options: null, storeName, cancellationToken);

        containerProvider
            .Received(6)
            .GetContainer<TestDocument>(storeName);

        container
            .ReceivedCalls()
            .Should()
            .HaveCount(6);
    }

    [Theory, AutoNSubstituteData]
    public void CreateQuery_Creates_QueryDefinition_From_Linq_Query(
        QueryExpression<TestDocument, TestDocument> query,
        string storeName,
        IQueryable<TestDocument> queryResult)
    {
        query
            .Invoke(Arg.Any<IQueryable<TestDocument>>())
            .Returns(queryResult);

        var result = sut.CreateQuery(query, storeName);

        containerProvider
            .Received(1)
            .GetContainer<TestDocument>(storeName);
        container
            .Received(1)
            .GetItemLinqQueryable<TestDocument>(
                linqSerializerOptions: Arg.Any<CosmosLinqSerializerOptions>());
        query
            .Received(1)
            .Invoke(queryable);

        linqQuery
            .Received(1)
            .GetQueryDefinition(queryResult);
    }
}
