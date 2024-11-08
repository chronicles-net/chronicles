using Chronicles.EventStore;
using Chronicles.EventStore.Internal;

namespace Chronicles.Tests.EventStore.Internal;

public class EventCatalogTests
{
    internal sealed record TestEvent();

    [Theory, AutoNSubstituteData]
    internal void GetConverter_Should_Return_Null_When_Event_Is_Unknown(
        EventCatalog sut)
        => sut.GetConverter("UnknownEvent")
            .Should()
            .BeNull();

    [Theory, AutoNSubstituteData]
    internal void GetConverter_Should_Return_Converter(
        IEventDataConverter converter)
    {
        var sut = new EventCatalog(
            new Dictionary<Type, (string Name, IEventDataConverter Converter)>
            {
                { typeof(TestEvent), (nameof(TestEvent), converter) }
            });

        sut.GetConverter(nameof(TestEvent))
            .Should()
            .Be(converter);
    }

    [Theory, AutoNSubstituteData]
    internal void GetEventName_Should_Return_Name(
        IEventDataConverter converter)
    {
        var sut = new EventCatalog(
            new Dictionary<Type, (string Name, IEventDataConverter Converter)>
            {
                { typeof(TestEvent), (nameof(TestEvent), converter) }
            });

        sut.GetEventName(typeof(TestEvent))
            .Should()
            .Be(nameof(TestEvent));
    }

    [Theory, AutoNSubstituteData]
    internal void GetEventName_Should_Throw_When_EventType_Is_Not_Known(
        IEventDataConverter converter)
    {
        var sut = new EventCatalog(
            new Dictionary<Type, (string Name, IEventDataConverter Converter)>
            {
                { typeof(TestEvent), (nameof(TestEvent), converter) }
            });

        FluentActions
            .Invoking(() => sut.GetEventName(typeof(EventCatalog)))
            .Should()
            .Throw<ArgumentException>();
    }
}