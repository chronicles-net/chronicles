using System.Text.Json;
using Chronicles.EventStore;
using Chronicles.EventStore.Internal;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Chronicles.Tests.EventStore.Internal;

public class StreamEventConverterTests
{
    internal sealed record EventData(string Name);

    [Theory, AutoNSubstituteData]
    internal void Convert_Should_Return_StreamEvent(
        [Frozen] IEventCatalog eventCatalog,
        EventMetadata eventMetadata,
        EventData eventData,
        IEventDataConverter converter,
        StreamEventConverter sut)
    {
        var json = """
            {
                "name": "test"
            }
            """;
        var context = new EventConverterContext(
            JsonDocument.Parse(json).RootElement,
            eventMetadata,
            new JsonSerializerOptions());

        converter
            .Convert(context)
            .Returns(eventData);

        eventCatalog
            .GetConverter(eventMetadata.Name)
            .Returns(converter);

        var result = sut.Convert(context);

        result
            .Data
            .Should()
            .BeEquivalentTo(eventData);

        result
            .Metadata
            .Should()
            .BeEquivalentTo(eventMetadata);
    }

    [Theory, AutoNSubstituteData]
    internal void Convert_Should_Return_Unknown_Event_When_EventName_Is_Unknown(
        [Frozen] IEventCatalog eventCatalog,
        EventMetadata eventMetadata,
        StreamEventConverter sut)
    {
        var json = """
            {
                "name": "test"
            }
            """;
        var context = new EventConverterContext(
            JsonDocument.Parse(json).RootElement,
            eventMetadata,
            new JsonSerializerOptions());

        eventCatalog
            .GetConverter(eventMetadata.Name)
            .Returns((IEventDataConverter?)null);

        var result = sut.Convert(context);

        result
            .Data
            .Should()
            .BeEquivalentTo(
                new UnknownEvent(json));

        result
            .Metadata
            .Should()
            .BeEquivalentTo(eventMetadata);
    }

    [Theory, AutoNSubstituteData]
    internal void Convert_Should_Return_FaultedEvent_On_Converter_Exception(
        [Frozen] IEventCatalog eventCatalog,
        EventMetadata eventMetadata,
        Exception exception,
        IEventDataConverter converter,
        StreamEventConverter sut)
    {
        var json = """
            {
                "name": "test"
            }
            """;
        var context = new EventConverterContext(
            JsonDocument.Parse(json).RootElement,
            eventMetadata,
            new JsonSerializerOptions());

        converter
            .Convert(context)
            .Throws(exception);

        eventCatalog
            .GetConverter(eventMetadata.Name)
            .Returns(converter);

        var result = sut.Convert(context);

        result
            .Data
            .Should()
            .BeEquivalentTo(
                new FaultedEvent(
                    context.Data.GetRawText(),
                    exception));

        result
            .Metadata
            .Should()
            .BeEquivalentTo(eventMetadata);
    }

    [Theory, AutoNSubstituteData]
    internal void Convert_Should_Return_Unknown_Event_When_Converter_Returns_Null(
        [Frozen] IEventCatalog eventCatalog,
        EventMetadata eventMetadata,
        IEventDataConverter converter,
        StreamEventConverter sut)
    {
        var json = """
            {
                "name": "test"
            }
            """;
        var context = new EventConverterContext(
            JsonDocument.Parse(json).RootElement,
            eventMetadata,
            new JsonSerializerOptions());

        converter
            .Convert(context)
            .Returns((object?)null);

        eventCatalog
            .GetConverter(eventMetadata.Name)
            .Returns(converter);

        var result = sut.Convert(context);

        result
            .Data
            .Should()
            .BeEquivalentTo(
                new UnknownEvent(json));

        result
            .Metadata
            .Should()
            .BeEquivalentTo(eventMetadata);
    }

    [Theory, AutoNSubstituteData]
    internal void Convert_Should_Return_FaultedEvent_On_EventCatalog_Exception(
        [Frozen] IEventCatalog eventCatalog,
        EventMetadata eventMetadata,
        Exception exception,
        StreamEventConverter sut)
    {
        var json = """
            {
                "name": "test"
            }
            """;
        var context = new EventConverterContext(
            JsonDocument.Parse(json).RootElement,
            eventMetadata,
            new JsonSerializerOptions());

        eventCatalog
            .GetConverter(eventMetadata.Name)
            .Throws(exception);

        var result = sut.Convert(context);

        result
            .Data
            .Should()
            .BeEquivalentTo(
                new FaultedEvent(
                    context.Data.GetRawText(),
                    exception));

        result
            .Metadata
            .Should()
            .BeEquivalentTo(eventMetadata);
    }
}