using System.Net;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using Chronicles.Documents;
using Chronicles.Documents.Internal;
using Microsoft.Azure.Cosmos;
using NSubstitute;

namespace Chronicles.Tests.Documents.Internal
{
    public class CosmosWriterTests
    {
        private readonly ItemResponse<TestDocument> documentResponse;
        private readonly TestDocument document;
        private readonly Container container;
        private readonly ICosmosContainerProvider containerProvider;
        private readonly ICosmosSerializer serializer;
        private readonly CosmosWriter<TestDocument> sut;

        public CosmosWriterTests()
        {
            var fixture = FixtureFactory.Create();
            document = fixture.Create<TestDocument>();

            container = Substitute.For<Container>();

            containerProvider = Substitute.For<ICosmosContainerProvider>();
            containerProvider
                .GetContainer<TestDocument>()
                .ReturnsForAnyArgs(container, null);

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

            documentResponse = Substitute.For<ItemResponse<TestDocument>>();
            documentResponse.Resource.Returns(document);
            documentResponse.ETag.Returns(fixture.Create<string>());
            container
                .ReadItemAsync<TestDocument>(default, default, default, default)
                .ReturnsForAnyArgs(documentResponse);

            serializer = Substitute.For<ICosmosSerializer>();
            serializer
                .FromString<TestDocument>(default)
                .ReturnsForAnyArgs(new Fixture().Create<TestDocument>());

            sut = new CosmosWriter<TestDocument>(containerProvider, serializer);
        }

        [Fact]
        public void Implements_Interface()
            => sut.Should().BeAssignableTo<IDocumentWriter<TestDocument>>();

        [Theory, AutoNSubstituteData]
        public async Task WriteAsync_Uses_The_Right_Container(
            ItemRequestOptions options,
            CancellationToken cancellationToken)
        {
            await sut.WriteAsync(document, options, cancellationToken);
            containerProvider
                .Received(1)
                .GetContainer<TestDocument>();
        }

        [Theory, AutoNSubstituteData]
        public async Task WriteAsync_UpsertItem_In_Container(
            ItemRequestOptions options,
            CancellationToken cancellationToken)
        {
            containerProvider
                .GetContainer<TestDocument>()
                .ReturnsForAnyArgs(container);

            await sut.WriteAsync(document, options, cancellationToken);
            await container
                .Received(1)
                .UpsertItemAsync<object>(
                    document,
                    new PartitionKey(document.Pk),
                    options,
                    cancellationToken);
        }

        [Theory, AutoNSubstituteData]
        public async Task ReplaceAsync_Calls_ReplaceItemAsync_On_Container(
            ItemRequestOptions options,
            CancellationToken cancellationToken)
        {
            await sut.ReplaceAsync(document, options, cancellationToken);
            _ = container
                .Received(1)
                .ReplaceItemAsync<object>(
                    document,
                    document.Id,
                    new PartitionKey(document.Pk),
                    options,
                    cancellationToken);
        }

        [Theory, AutoNSubstituteData]
        public async Task DeleteAsync_Calls_DeleteItemAsync_On_Container(
            ItemRequestOptions options,
            CancellationToken cancellationToken)
        {
            await sut.DeleteAsync(document.Id, document.Pk, options, cancellationToken);
            _ = container
                .Received(1)
                .DeleteItemAsync<object>(
                    document.Id,
                    new PartitionKey(document.Pk),
                    options,
                    cancellationToken: cancellationToken);
        }

        [Theory, AutoNSubstituteData]
        public void Multiple_Operations_Uses_Same_Container(
            ItemRequestOptions options,
            CancellationToken cancellationToken)
        {
            _ = sut.WriteAsync(document, options, cancellationToken);
            _ = sut.WriteAsync(document, options, cancellationToken);
            _ = sut.CreateAsync(document, options, cancellationToken);
            _ = sut.CreateAsync(document, options, cancellationToken);
            _ = sut.ReplaceAsync(document, options, cancellationToken);
            _ = sut.ReplaceAsync(document, options, cancellationToken);
            _ = sut.DeleteAsync(document.Id, document.Pk, options, cancellationToken);
            _ = sut.DeleteAsync(document.Id, document.Pk, options, cancellationToken);

            container
                .ReceivedCalls()
                .Should()
                .HaveCount(8);
        }

        [Theory, AutoNSubstituteData]
        public async Task UpdateAsync_Reads_The_Document(
            string documentId,
            string partitionKey,
            Func<TestDocument, Task> updateDocument,
            int retries,
            CancellationToken cancellationToken)
        {
            await sut.UpdateAsync(
                documentId,
                partitionKey,
                updateDocument,
                retries,
                cancellationToken);

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
            [Substitute] Func<TestDocument, Task> updateDocument,
            int retries,
            CancellationToken cancellationToken)
        {
            await sut.UpdateAsync(
                documentId,
                partitionKey,
                updateDocument,
                retries,
                cancellationToken);

            _ = updateDocument
                .Received(1)
                .Invoke(document);
        }

        [Theory, AutoNSubstituteData]
        public async Task UpdateAsync_Calls_ReplaceItem_With_Updated_Document(
            string documentId,
            string partitionKey,
            [Substitute] Func<TestDocument, Task> updateDocument,
            int retries,
            CancellationToken cancellationToken)
        {
            await sut.UpdateAsync(
                documentId,
                partitionKey,
                updateDocument,
                retries,
                cancellationToken);

            _ = container
                .Received(1)
                .ReplaceItemAsync<object>(
                    document,
                    document.Id,
                    new PartitionKey(document.Pk),
                    Arg.Is<ItemRequestOptions>(o => o.IfMatchEtag == documentResponse.ETag),
                    cancellationToken);
        }

        [Theory, AutoNSubstituteData]
        public async Task UpdateOrCreateAsync_Reads_The_Document(
           Func<TestDocument, Task> updateDocument,
           int retries,
           TestDocument defaultDocument,
           CancellationToken cancellationToken)
        {
            await sut.UpdateOrCreateAsync(
                () => defaultDocument,
                updateDocument,
                retries,
                cancellationToken);

            _ = container
                .Received(1)
                .ReadItemAsync<TestDocument>(
                    defaultDocument.Id,
                    new PartitionKey(defaultDocument.Pk),
                    cancellationToken: cancellationToken);
        }

        [Theory, AutoNSubstituteData]
        public async Task UpdateAsync_Calls_UpdateDocument_With_Found_Document(
            [Substitute] Action<TestDocument> updateDocument,
            int retries,
            TestDocument defaultDocument,
            CancellationToken cancellationToken)
        {
            await sut.UpdateOrCreateAsync(
                () => defaultDocument,
                updateDocument,
                retries,
                cancellationToken);

            updateDocument
                .Received(1)
                .Invoke(document);
        }

        [Theory, AutoNSubstituteData]
        public async Task UpdateAsync_Calls_UpdateDocument_With_Default_Document_If_Not_Found(
            [Substitute] Func<TestDocument, Task> updateDocument,
            int retries,
            TestDocument defaultDocument,
            CancellationToken cancellationToken)
        {
            _ = container
                .ReadItemAsync<TestDocument>(defaultDocument.Id, default, default, default)
                .ReturnsForAnyArgs<ItemResponse<TestDocument>>(c
                    => throw new CosmosException("fake", HttpStatusCode.NotFound, 0, "1", 1));

            await sut.UpdateOrCreateAsync(
                () => defaultDocument,
                updateDocument,
                retries,
                cancellationToken);

            _ = updateDocument
                .Received(1)
                .Invoke(defaultDocument);
        }

        [Theory, AutoNSubstituteData]
        public async Task UpdateOrCreateAsync_Calls_ReplaceItem_If_Document_Was_Found(
            [Substitute] Action<TestDocument> updateDocument,
            int retries,
            TestDocument defaultDocument,
            CancellationToken cancellationToken)
        {
            await sut.UpdateOrCreateAsync(
                () => defaultDocument,
                updateDocument,
                retries,
                cancellationToken);

            _ = container
                .Received(1)
                .ReplaceItemAsync<object>(
                    document,
                    document.Id,
                    new PartitionKey(document.Pk),
                    Arg.Is<ItemRequestOptions>(o => o.IfMatchEtag == documentResponse.ETag),
                    cancellationToken);
        }

        [Theory, AutoNSubstituteData]
        public async Task UpdateOrCreateAsync_Calls_CreateItem_If_Document_Was_Not_Found(
            [Substitute] Action<TestDocument> updateDocument,
            int retries,
            TestDocument defaultDocument,
            CancellationToken cancellationToken)
        {
            _ = container
                .ReadItemAsync<TestDocument>(defaultDocument.Id, default, default, default)
                .ReturnsForAnyArgs<ItemResponse<TestDocument>>(c
                    => throw new CosmosException("fake", HttpStatusCode.NotFound, 0, "1", 1));

            await sut.UpdateOrCreateAsync(
                () => defaultDocument,
                updateDocument,
                retries,
                cancellationToken);

            _ = container
                .Received(1)
                .CreateItemAsync<object>(
                    defaultDocument,
                    new PartitionKey(defaultDocument.Pk),
                    Arg.Any<ItemRequestOptions>(),
                    cancellationToken);
        }
    }
}