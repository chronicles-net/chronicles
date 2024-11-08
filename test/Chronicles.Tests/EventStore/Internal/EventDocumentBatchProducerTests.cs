using Chronicles.EventStore;
using Chronicles.EventStore.Internal;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Chronicles.Tests.EventStore.Internal;

public class EventDocumentBatchProducerTests
{
    public record EventData(string Name);

    [Theory, AutoNSubstituteData]
    internal void Should_Produce_EventDocumentBatch(
        IEventCatalogFactory eventCatalogFactory,
        IEventCatalog eventCatalog,
        StreamMetadataDocument metadata,
        IReadOnlyCollection<EventData> eventData)
    {
        var dateTimeProvider = new FakeTimeProvider();
        dateTimeProvider.AutoAdvanceAmount = TimeSpan.Zero;

        metadata = metadata with { Version = 0, State = StreamState.New };
        eventCatalogFactory
            .Get(default)
            .ReturnsForAnyArgs(eventCatalog);

        eventCatalog
            .GetEventName(typeof(EventData))
            .Returns("EventData");

        var batch = new EventDocumentBatchProducer(dateTimeProvider, eventCatalogFactory)
            .FromEvents(
                eventData,
                metadata,
                storeName: null,
                new StreamWriteOptions()
                {
                    CorrelationId = "correlationId",
                    CausationId = "causationId"
                });

        batch.Metadata
            .State
            .Should()
            .Be(StreamState.Active);
        batch.Metadata
            .StreamId
            .Should()
            .Be(metadata.StreamId);
        batch.Metadata
            .Version
            .Should()
            .Be(eventData.Count);
        batch.Metadata
            .Timestamp
            .Should()
            .Be(dateTimeProvider.GetUtcNow());

        var version = 0;
        batch.Events
            .Should()
            .AllSatisfy(evt =>
            {
                version++;
                evt.Id
                    .Should()
                    .Be(version.ToString());
                evt.Pk
                    .Should()
                    .Be((string)batch.Metadata.StreamId);
                evt.Data
                    .Should()
                    .BeOfType<EventData>()
                    .Subject.Should().Be(eventData.Skip(version - 1).First());
                evt.Properties
                    .StreamId.Should().Be(metadata.StreamId);
                evt.Properties
                    .Name.Should().Be("EventData");
                evt.Properties
                    .CausationId.Should().Be("causationId");
                evt.Properties
                    .CorrelationId.Should().Be("correlationId");
                evt.Properties
                    .Timestamp.Should().Be(batch.Metadata.Timestamp);
                evt.Properties
                    .Version.Should().Be(version);
            });
    }
}
