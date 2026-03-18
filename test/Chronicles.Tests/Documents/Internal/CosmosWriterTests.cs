using System.Net;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using Chronicles.Documents;
using Chronicles.Documents.Internal;
using Microsoft.Azure.Cosmos;

namespace Chronicles.Tests.Documents.Internal;

public class CosmosWriterTests
{
    private readonly ItemResponse<TestDocument> documentResponse;
    private readonly TestDocument document;
    private readonly Container container;
    private readonly ICosmosContainerProvider containerProvider;
    private readonly CosmosWriter<TestDocument> sut;

    public CosmosWriterTests()
    {
        var fixture = FixtureFactory.Create();
        document = fixture.Create<TestDocument>();

        container = Substitute.For<Container>();

        containerProvider = Substitute.For<ICosmosContainerProvider>();
        containerProvider
            .GetContainer<TestDocument>(default)
            .ReturnsForAnyArgs(container);

        var response = Substitute.For<ItemResponse<object>>();
        response.Resource.Returns(fixture.Create<string>());

        container
            .CreateItemAsync<object>(default, default, default, default)
            .ReturnsForAnyArgs(response);
        container
            .ReplaceItemAsync<object>(default, default, default, default, default)
            .ReturnsForAnyArgs(response);
        container
            .UpsertItemAsync<object>(default, default, default, default)
            .ReturnsForAnyArgs(response);
        container
            .PatchItemAsync<object>(default, default, default, default)
            .ReturnsForAnyArgs(response);

        var deleteResponse = new ResponseMessage(HttpStatusCode.OK);
        container
            .DeleteAllItemsByPartitionKeyStreamAsync(default, default, default)
            .ReturnsForAnyArgs(deleteResponse);

        documentResponse = Substitute.For<ItemResponse<TestDocument>>();
        documentResponse.Resource.Returns(document);
        documentResponse.ETag.Returns(fixture.Create<string>());
        container
            .ReadItemAsync<TestDocument>(default, default, default, default)
            .ReturnsForAnyArgs(documentResponse);

        sut = new CosmosWriter<TestDocument>(containerProvider);
    }

    [Fact]
    public void Implements_Interface()
        => sut.Should().BeAssignableTo<IDocumentWriter<TestDocument>>();

    [Theory, AutoNSubstituteData]
    public async Task WriteAsync_Uses_The_Right_Container(
        ItemRequestOptions options,
        string storeName,
        CancellationToken cancellationToken)
    {
        await sut.WriteAsync(document, options, storeName, cancellationToken);

        containerProvider
            .Received(1)
            .GetContainer<TestDocument>(storeName);
        containerProvider
            .Received(1)
            .GetContainer<TestDocument>(storeName);
    }

    [Theory, AutoNSubstituteData]
    public async Task WriteAsync_UpsertItem_In_Container(
        ItemRequestOptions options,
        string storeName,
        CancellationToken cancellationToken)
    {
        containerProvider
            .GetContainer<TestDocument>()
            .ReturnsForAnyArgs(container);

        await sut.WriteAsync(document, options, storeName, cancellationToken);

        containerProvider
            .Received(1)
            .GetContainer<TestDocument>(storeName);
        await container
            .Received(1)
            .UpsertItemAsync(
                document,
                new PartitionKey(document.Pk),
                options,
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public async Task ReplaceAsync_Calls_ReplaceItemAsync_On_Container(
        ItemRequestOptions options,
        string storeName,
        CancellationToken cancellationToken)
    {
        await sut.ReplaceAsync(document, options, storeName, cancellationToken);

        containerProvider
            .Received(1)
            .GetContainer<TestDocument>(storeName);
        _ = container
            .Received(1)
            .ReplaceItemAsync(
                document,
                document.Id,
                new PartitionKey(document.Pk),
                options,
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public async Task DeleteAsync_Calls_DeleteItemAsync_On_Container(
        ItemRequestOptions options,
        string storeName,
        CancellationToken cancellationToken)
    {
        await sut.DeleteAsync(document.Id, document.Pk, options, storeName, cancellationToken);

        containerProvider
            .Received(1)
            .GetContainer<TestDocument>(storeName);
        _ = container
            .Received(1)
            .DeleteItemAsync<TestDocument>(
                document.Id,
                new PartitionKey(document.Pk),
                options,
                cancellationToken: cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public async Task DeletePartitionAsync_Calls_DeleteAllItemsByPartitionKeyStreamAsync_On_Container(
        ItemRequestOptions options,
        string storeName,
        CancellationToken cancellationToken)
    {
        await sut.DeletePartitionAsync(document.Pk, options, storeName, cancellationToken);

        containerProvider
            .Received(1)
            .GetContainer<TestDocument>(storeName);
        _ = container
            .Received(1)
            .DeleteAllItemsByPartitionKeyStreamAsync(
                new PartitionKey(document.Pk),
                options,
                cancellationToken: cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public void Multiple_Operations_Uses_Same_Container(
        ItemRequestOptions options,
        string storeName,
        CancellationToken cancellationToken)
    {
        _ = sut.WriteAsync(document, options, storeName, cancellationToken);
        _ = sut.WriteAsync(document, options, storeName, cancellationToken);
        _ = sut.CreateAsync(document, options, storeName, cancellationToken);
        _ = sut.CreateAsync(document, options, storeName, cancellationToken);
        _ = sut.ReplaceAsync(document, options, storeName, cancellationToken);
        _ = sut.ReplaceAsync(document, options, storeName, cancellationToken);
        _ = sut.DeleteAsync(document.Id, document.Pk, options, storeName, cancellationToken);
        _ = sut.DeleteAsync(document.Id, document.Pk, options, storeName, cancellationToken);

        containerProvider
            .Received(8)
            .GetContainer<TestDocument>(storeName);
        container
            .ReceivedCalls()
            .Should()
            .HaveCount(8);
    }

    [Theory, AutoNSubstituteData]
    public async Task UpdateAsync_Reads_The_Document(
        string documentId,
        string partitionKey,
        Func<TestDocument, Task<TestDocument>> updateDocument,
        int retries,
        string storeName,
        CancellationToken cancellationToken)
    {
        updateDocument
            .Invoke(document)
            .ReturnsForAnyArgs(document);

        await sut.UpdateAsync(
            documentId,
            partitionKey,
            updateDocument,
            retries,
            storeName,
            cancellationToken);

        containerProvider
            .Received()
            .GetContainer<TestDocument>(storeName);
        _ = container
            .Received(1)
            .ReadItemAsync<TestDocument>(
                documentId,
                new PartitionKey(partitionKey),
                cancellationToken: cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public async Task UpdateAsync_Calls_UpdateDocument_With_Read_Document(
        string documentId,
        string partitionKey,
        [Substitute] Func<TestDocument, Task<TestDocument>> updateDocument,
        int retries,
        string storeName,
        CancellationToken cancellationToken)
    {
        updateDocument
            .Invoke(document)
            .ReturnsForAnyArgs(document);

        await sut.UpdateAsync(
            documentId,
            partitionKey,
            updateDocument,
            retries,
            storeName,
            cancellationToken);

        _ = updateDocument
            .Received(1)
            .Invoke(document);
    }

    [Theory, AutoNSubstituteData]
    public async Task UpdateAsync_Calls_ReplaceItem_With_Updated_Document(
        string documentId,
        string partitionKey,
        [Substitute] Func<TestDocument, Task<TestDocument>> updateDocument,
        int retries,
        string storeName,
        CancellationToken cancellationToken)
    {
        updateDocument
            .Invoke(document)
            .ReturnsForAnyArgs(document);

        await sut.UpdateAsync(
            documentId,
            partitionKey,
            updateDocument,
            retries,
            storeName,
            cancellationToken);

        containerProvider
            .Received()
            .GetContainer<TestDocument>(storeName);
        _ = container
            .Received(1)
            .ReplaceItemAsync(
                document,
                document.Id,
                new PartitionKey(document.Pk),
                Arg.Is<ItemRequestOptions>(o => o.IfMatchEtag == documentResponse.ETag),
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public async Task UpdateOrCreateAsync_Reads_The_Document(
       Func<TestDocument, Task<TestDocument>> updateDocument,
       int retries,
       string storeName,
       TestDocument defaultDocument,
       CancellationToken cancellationToken)
    {
        updateDocument
            .Invoke(document)
            .ReturnsForAnyArgs(document);

        await sut.UpdateOrCreateAsync(
            () => defaultDocument,
            updateDocument,
            retries,
            storeName,
            cancellationToken);

        containerProvider
            .Received()
            .GetContainer<TestDocument>(storeName);
        _ = container
            .Received(1)
            .ReadItemAsync<TestDocument>(
                defaultDocument.Id,
                new PartitionKey(defaultDocument.Pk),
                cancellationToken: cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public async Task UpdateAsync_Calls_UpdateDocument_With_Found_Document(
        [Substitute] Func<TestDocument, TestDocument> updateDocument,
        int retries,
        string storeName,
        TestDocument defaultDocument,
        CancellationToken cancellationToken)
    {
        updateDocument
            .Invoke(document)
            .ReturnsForAnyArgs(document);

        await sut.UpdateOrCreateAsync(
            () => defaultDocument,
            updateDocument,
            retries,
            storeName,
            cancellationToken);

        updateDocument
            .Received(1)
            .Invoke(document);
    }

    [Theory, AutoNSubstituteData]
    public async Task UpdateAsync_Calls_UpdateDocument_With_Default_Document_If_Not_Found(
        [Substitute] Func<TestDocument, Task<TestDocument>> updateDocument,
        int retries,
        string storeName,
        TestDocument defaultDocument,
        CancellationToken cancellationToken)
    {
        updateDocument
            .Invoke(document)
            .ReturnsForAnyArgs(document);
        _ = container
            .ReadItemAsync<TestDocument>(defaultDocument.Id, default, default, default)
            .ReturnsForAnyArgs<ItemResponse<TestDocument>>(c
                => throw new CosmosException("fake", HttpStatusCode.NotFound, 0, "1", 1));

        await sut.UpdateOrCreateAsync(
            () => defaultDocument,
            updateDocument,
            retries,
            storeName,
            cancellationToken);

        _ = updateDocument
            .Received(1)
            .Invoke(defaultDocument);
    }

    [Theory, AutoNSubstituteData]
    public async Task UpdateOrCreateAsync_Calls_ReplaceItem_If_Document_Was_Found(
        [Substitute] Func<TestDocument, TestDocument> updateDocument,
        int retries,
        string storeName,
        TestDocument defaultDocument,
        CancellationToken cancellationToken)
    {
        updateDocument
            .Invoke(document)
            .ReturnsForAnyArgs(document);

        await sut.UpdateOrCreateAsync(
            () => defaultDocument,
            updateDocument,
            retries,
            storeName,
            cancellationToken);

        containerProvider
            .Received()
            .GetContainer<TestDocument>(storeName);
        _ = container
            .Received(1)
            .ReplaceItemAsync(
                document,
                document.Id,
                new PartitionKey(document.Pk),
                Arg.Is<ItemRequestOptions>(o => o.IfMatchEtag == documentResponse.ETag),
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public async Task UpdateOrCreateAsync_Calls_CreateItem_If_Document_Was_Not_Found(
        [Substitute] Func<TestDocument, TestDocument> updateDocument,
        int retries,
        string storeName,
        TestDocument defaultDocument,
        CancellationToken cancellationToken)
    {
        updateDocument
            .Invoke(defaultDocument)
            .ReturnsForAnyArgs(defaultDocument);
        _ = container
            .ReadItemAsync<TestDocument>(defaultDocument.Id, default, default, default)
            .ReturnsForAnyArgs<ItemResponse<TestDocument>>(c
                => throw new CosmosException("fake", HttpStatusCode.NotFound, 0, "1", 1));

        await sut.UpdateOrCreateAsync(
            () => defaultDocument,
            updateDocument,
            retries,
            storeName,
            cancellationToken);

        containerProvider
            .Received()
            .GetContainer<TestDocument>(storeName);
        _ = container
            .Received(1)
            .CreateItemAsync(
                defaultDocument,
                new PartitionKey(defaultDocument.Pk),
                Arg.Any<ItemRequestOptions>(),
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public void CreateTransaction_Calls_CreateTransactionalBatch_On_Container(
        string partitionKey,
        string storeName)
    {
        sut.CreateTransaction(partitionKey, storeName);

        containerProvider
            .Received(1)
            .GetContainer<TestDocument>(storeName);
        container
            .Received(1)
            .CreateTransactionalBatch(
                new PartitionKey(partitionKey));
    }


    [Theory, AutoNSubstituteData]
    public void CreateTransaction_Returns_CosmosTransaction(
        string partitionKey,
        string storeName)
    {
        var result = sut.CreateTransaction(partitionKey, storeName);
        result
            .Should()
            .BeAssignableTo<CosmosTransaction<TestDocument>>();
    }

    [Theory, AutoNSubstituteData]
    public async Task ConditionalUpdateAsync_Returns_Null_When_Condition_Is_Not_Met(
        [Substitute] Func<TestDocument, Task<TestDocument>> updateDocument,
        string storeName,
        TestDocument defaultDocument,
        CancellationToken cancellationToken)
    {
        var response = await sut.ConditionalUpdateAsync(
            defaultDocument.Id,
            defaultDocument.Pk,
            condition: d => false,
            updateDocument,
            storeName,
            cancellationToken);

        response.Should().BeNull();

        _ = updateDocument
            .DidNotReceive()
            .Invoke(Arg.Any<TestDocument>());

        _ = container
            .DidNotReceive()
            .ReplaceItemAsync(
                Arg.Any<TestDocument>(),
                Arg.Any<string>(),
                Arg.Any<PartitionKey>(),
                Arg.Any<ItemRequestOptions>(),
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public async Task ConditionalUpdateAsync_Calls_ReplaceItem_If_Document_Was_Found(
        [Substitute] Func<TestDocument, Task<TestDocument>> updateDocument,
        string storeName,
        TestDocument defaultDocument,
        CancellationToken cancellationToken)
    {
        updateDocument
            .Invoke(document)
            .ReturnsForAnyArgs(document);

        await sut.ConditionalUpdateAsync(
            defaultDocument.Id,
            defaultDocument.Pk,
            condition: d => true,
            updateDocument,
            storeName,
            cancellationToken);

        containerProvider
            .Received()
            .GetContainer<TestDocument>(storeName);
        _ = container
            .Received(1)
            .ReplaceItemAsync(
                document,
                document.Id,
                new PartitionKey(document.Pk),
                Arg.Is<ItemRequestOptions>(o => o.IfMatchEtag == documentResponse.ETag),
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public async Task ConditionalUpdateAsync_Calls_UpdateDocument_If_Document_Was_Found(
        [Substitute] Func<TestDocument, Task<TestDocument>> updateDocument,
        string storeName,
        TestDocument defaultDocument,
        CancellationToken cancellationToken)
    {
        updateDocument
            .Invoke(document)
            .ReturnsForAnyArgs(document);

        await sut.ConditionalUpdateAsync(
            defaultDocument.Id,
            defaultDocument.Pk,
            condition: d => true,
            updateDocument,
            storeName,
            cancellationToken);

        _ = updateDocument
            .Received(1)
            .Invoke(document);
    }

    [Theory, AutoNSubstituteData]
    public async Task ConditionalUpdateAsync_Should_Catch_Cosmos_NotFound_Exception(
        [Substitute] Func<TestDocument, Task<TestDocument>> updateDocument,
        string storeName,
        TestDocument defaultDocument,
        CancellationToken cancellationToken)
    {
        _ = container
            .ReadItemAsync<TestDocument>(defaultDocument.Id, default, default, default)
            .ReturnsForAnyArgs<ItemResponse<TestDocument>>(c
                => throw new CosmosException("fake", HttpStatusCode.NotFound, 0, "1", 1));

        updateDocument
            .Invoke(document)
            .ReturnsForAnyArgs(document);

        var response = await sut.ConditionalUpdateAsync(
            defaultDocument.Id,
            defaultDocument.Pk,
            condition: d => true,
            updateDocument,
            storeName,
            cancellationToken);

        response.Should().BeNull();
    }

    [Theory, AutoNSubstituteData]
    public async Task ConditionalUpdateAsync_Should_Catch_Cosmos_PreconditionFailed_Exception(
        [Substitute] Func<TestDocument, Task<TestDocument>> updateDocument,
        string storeName,
        TestDocument defaultDocument,
        CancellationToken cancellationToken)
    {
        _ = container
            .ReplaceItemAsync(defaultDocument, default, default, default, default)
            .ReturnsForAnyArgs<ItemResponse<TestDocument>>(c
                => throw new CosmosException("fake", HttpStatusCode.PreconditionFailed, 0, "1", 1));

        updateDocument
            .Invoke(document)
            .ReturnsForAnyArgs(document);

        var response = await sut.ConditionalUpdateAsync(
            defaultDocument.Id,
            defaultDocument.Pk,
            condition: d => true,
            updateDocument,
            storeName,
            cancellationToken);

        response.Should().BeNull();
    }
}
