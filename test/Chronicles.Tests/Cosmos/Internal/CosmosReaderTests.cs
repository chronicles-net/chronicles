using AutoFixture;
using Chronicles.Cosmos;
using Chronicles.Cosmos.Internal;
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
            .GetQueryDefinition<TestDocument>(default)
            .ReturnsForAnyArgs(query);
        linqQuery
            .GetFeedIterator<TestDocument>(default)
            .ReturnsForAnyArgs(feedIterator);

        sut = new CosmosReader<TestDocument>(containerProvider, linqQuery);
    }

    [Fact]
    public void Implements_Interface()
        => sut.Should().BeAssignableTo<ICosmosReader<TestDocument>>();

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
    public async Task FindAsync_Uses_The_Right_Container(
        string documentId,
        string partitionKey,
        ItemRequestOptions options,
        CancellationToken cancellationToken)
    {
        await sut.FindAsync<TestDocument>(
            documentId,
            partitionKey,
            options,
            cancellationToken);

        containerProvider
            .Received(1)
            .GetContainer<TestDocument>();
    }

    [Theory, AutoNSubstituteData]
    public async Task FindAsync_Return_Default_When_TestDocument_IsNot_Found(
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
        container
            .ReadItemAsync<TestDocument>(default, default, default, default)
            .ReturnsForAnyArgs(Task.FromException<ItemResponse<TestDocument>>(exception));

        var response = await sut.FindAsync<TestDocument>(
            documentId,
            partitionKey,
            options,
            cancellationToken);

        response
            .Should()
            .BeNull();
    }

    [Theory, AutoNSubstituteData]
    public async Task FindAsync_Returns_TestDocument_When_Successful(
        string partitionKey,
        string documentId,
        ItemRequestOptions options,
        CancellationToken cancellationToken)
    {
        var result = await sut.FindAsync<TestDocument>(
            documentId,
            partitionKey,
            options,
            cancellationToken);
        result
            .Should()
            .Be(document);
    }

    [Theory, AutoNSubstituteData]
    public void ReadAllAsync_Uses_The_Right_Container(
        string partitionKey,
        QueryRequestOptions options,
        CancellationToken cancellationToken)
    {
        _ = sut.ReadAllAsync(
            partitionKey,
            options,
            cancellationToken);

        containerProvider
            .Received(1)
            .GetContainer<TestDocument>();
    }

    [Theory, AutoNSubstituteData]
    public async Task ReadAllAsync_Executes_Linq_Query(
        string partitionKey,
        QueryRequestOptions options,
        CancellationToken cancellationToken)
    {
        await sut
            .ReadAllAsync(
                partitionKey,
                options,
                cancellationToken)
            .ToListAsync(cancellationToken);

        container
            .Received(1)
            .GetItemLinqQueryable<TestDocument>(
                requestOptions: Arg.Is<QueryRequestOptions>(o
                    => o.PartitionKey == new PartitionKey(partitionKey)));

        container
            .ReceivedCallWithArgument<QueryRequestOptions>()
            .Should()
            .BeEquivalentTo(options, o => o.Excluding(i => i.PartitionKey));

        linqQuery
            .Received(1)
            .GetFeedIterator(queryable);
    }

    [Theory, AutoNSubstituteData]
    public async Task ReadAllAsync_Returns_Empty_No_More_Result(
        string partitionKey,
        QueryRequestOptions options,
        CancellationToken cancellationToken)
    {
        feedIterator.HasMoreResults.Returns(false);

        var response = await sut
            .ReadAllAsync(
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
    public async Task ReadAllAsync_Returns_Empty_When_Query_Matches_Non(
        string partitionKey,
        QueryRequestOptions options,
        CancellationToken cancellationToken)
    {
        feedIterator.HasMoreResults.Returns(true, false);

        var response = await sut
            .ReadAllAsync(partitionKey, options, cancellationToken)
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
    public async Task ReadAllAsync_Returns_All_Items(
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

        var response = await sut
            .ReadAllAsync(
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
}
