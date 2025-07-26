using System.Collections.Immutable;
using System.Text.Json;
using Chronicles.Documents;
using Chronicles.EventStore;
using Chronicles.EventStore.Internal;
using Chronicles.EventStore.Internal.Converters;
using Chronicles.Testing;
using Microsoft.Azure.Cosmos;
using NSubstitute;

namespace Chronicles.Tests.EventStore.Internal;

public class EventDocumentReaderTests
{
    private const string EventName = "evt1";

    internal sealed record SampleEvent(string Name);

    private readonly JsonSerializerOptions options = new JsonSerializerOptions();
    private readonly EventCatalog catalog =
        new EventCatalog(
            new Dictionary<Type, (string Name, IEventDataConverter Converter)>
            {
                [typeof(SampleEvent)] = (EventName, new EventDataConverter(EventName, typeof(SampleEvent))),
            });

    public EventDocumentReaderTests()
    {
        options.Converters.Add(new StreamVersionJsonConverter());
        options.Converters.Add(new StreamIdJsonConverter());
        options.Converters.Add(
            new StreamEventJsonConverter(
                new StreamEventConverter(
                    catalog)));
    }

    [Theory, AutoNSubstituteData]
    internal async Task ReadAsync_Should_Read_All_EventDocuments(
        StreamMetadataDocument metadata,
        EventDocument[] documents,
        SampleEvent[] sampleEvents,
        CancellationToken cancellationToken)
    {
        var streamId = new StreamId("stream", "1");

        var store = FakeDocumentStoreProvider.FromOptions(options);
        var reader = new FakeDocumentReader<EventDocument>(store);

        await store.GetStore(null)
            .GetContainer<EventDocument>()
            .GetOrCreatePartition((string)metadata.StreamId)
            .UpsertDocuments(documents.Select((d, idx) => d with
            {
                Pk = (string)metadata.StreamId,
                Properties = d.Properties with
                {
                    Name = EventName,
                    Version = idx + 1,
                },
                Data = sampleEvents[idx],
            }).ToImmutableList());

        var sut = new EventDocumentReader(reader);
        var events = sut.ReadAsync(
            metadata with { State = StreamState.Active },
            options: null,
            storeName: null,
            cancellationToken);
        await foreach (var evt in events)
        {
            sampleEvents.Should().Contain((SampleEvent)evt.Data);
        }
    }

    [Theory, AutoNSubstituteData]
    internal async Task ReadAsync_Should_Read_FromVersion(
        StreamMetadataDocument metadata,
        EventDocument[] documents,
        SampleEvent[] sampleEvents,
        CancellationToken cancellationToken)
    {
        var streamId = new StreamId("stream", "1");
        var store = FakeDocumentStoreProvider.FromOptions(options);
        var reader = new FakeDocumentReader<EventDocument>(store);

        await store.GetStore(null)
            .GetContainer<EventDocument>()
            .GetOrCreatePartition((string)metadata.StreamId)
            .UpsertDocuments(documents.Select((d, idx) => d with
            {
                Pk = (string)metadata.StreamId,
                Properties = d.Properties with
                {
                    Name = EventName,
                    Version = idx + 1,
                },
                Data = sampleEvents[idx],
            }).ToImmutableList());

        var sut = new EventDocumentReader(reader);
        var events = sut.ReadAsync(
            metadata with { State = StreamState.Active },
            options: new() { FromVersion = 1 },
            storeName: null,
            cancellationToken);
        await foreach (var evt in events)
        {
            evt.Metadata.Version.Should().BeGreaterThanOrEqualTo(1);
        }
    }

    [Theory, AutoNSubstituteData]
    internal async Task ReadAsync_On_Empty_Stream_Should_Not_Query_Documents(
        [Frozen] IDocumentReader<EventDocument> streamReader,
        EventDocumentReader sut,
        StreamMetadataDocument metadata,
        CancellationToken cancellationToken)
    {
        var events = sut.ReadAsync(
            metadata with { State = StreamState.New, Version = 0 },
            options: null,
            storeName: null,
            cancellationToken);
        await foreach (var _ in events)
        {
        }

        _ = streamReader
            .DidNotReceive()
            .QueryAsync<StreamEvent>(
                Arg.Any<QueryDefinition>(),
                Arg.Any<string>(),
                Arg.Any<QueryRequestOptions?>(),
                Arg.Any<string?>(),
                cancellationToken);
    }

    [Theory]
    [InlineAutoNSubstituteData(null)]
    [InlineAutoNSubstituteData("my-store-name")]
    internal async Task ReadAsync_From_Store_Should_Query_Documents(
        string? storeName,
        [Frozen] IDocumentReader<EventDocument> streamReader,
        EventDocumentReader sut,
        StreamMetadataDocument metadata,
        CancellationToken cancellationToken)
    {
        var events = sut.ReadAsync(
            metadata with { State = StreamState.Active, Version = 1 },
            options: null,
            storeName: storeName,
            cancellationToken);
        await foreach (var _ in events)
        {
        }

        _ = streamReader
            .Received(1)
            .QueryAsync<StreamEvent>(
                Arg.Any<QueryDefinition>(),
                Arg.Any<string?>(),
                Arg.Any<QueryRequestOptions?>(),
                Arg.Is<string?>(s => s == storeName),
                cancellationToken);
    }
}
