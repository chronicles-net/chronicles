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

    // === ALIAS TESTS (require Gurney's EventCatalog constructor change) ===
    // These tests assume EventCatalog gains a second constructor parameter:
    //   IDictionary<string, IEventDataConverter>? aliasToConverterMappings
    // Remove the #if guard when Gurney's alias implementation is merged.

#if ENABLE_ALIAS_TESTS

    [Theory, AutoNSubstituteData]
    internal void GetConverter_Should_Return_Converter_For_Alias(
        IEventDataConverter converter)
    {
        var sut = new EventCatalog(
            new Dictionary<Type, (string Name, IEventDataConverter Converter)>
            {
                { typeof(TestEvent), (nameof(TestEvent), converter) }
            },
            new Dictionary<string, IEventDataConverter>
            {
                { "OldTestEvent", converter }
            });

        sut.GetConverter("OldTestEvent")
            .Should()
            .Be(converter);
    }

    [Theory, AutoNSubstituteData]
    internal void GetConverter_Should_Return_Same_Converter_For_Primary_And_Alias(
        IEventDataConverter converter)
    {
        var sut = new EventCatalog(
            new Dictionary<Type, (string Name, IEventDataConverter Converter)>
            {
                { typeof(TestEvent), (nameof(TestEvent), converter) }
            },
            new Dictionary<string, IEventDataConverter>
            {
                { "OldTestEvent", converter }
            });

        var primaryResult = sut.GetConverter(nameof(TestEvent));
        var aliasResult = sut.GetConverter("OldTestEvent");

        primaryResult
            .Should()
            .Be(aliasResult);
    }

    [Theory, AutoNSubstituteData]
    internal void GetEventName_Should_Return_Primary_Name_Even_When_Aliases_Exist(
        IEventDataConverter converter)
    {
        var sut = new EventCatalog(
            new Dictionary<Type, (string Name, IEventDataConverter Converter)>
            {
                { typeof(TestEvent), (nameof(TestEvent), converter) }
            },
            new Dictionary<string, IEventDataConverter>
            {
                { "OldTestEvent", converter }
            });

        sut.GetEventName(typeof(TestEvent))
            .Should()
            .Be(nameof(TestEvent));
    }

    [Theory, AutoNSubstituteData]
    internal void Constructor_Should_Throw_On_Duplicate_Alias(
        IEventDataConverter converter)
    {
        var typeToNameMappings = new Dictionary<Type, (string Name, IEventDataConverter Converter)>
        {
            { typeof(TestEvent), (nameof(TestEvent), converter) }
        };

        FluentActions
            .Invoking(() => new EventCatalog(
                typeToNameMappings,
                new Dictionary<string, IEventDataConverter>
                {
                    { nameof(TestEvent), converter }
                }))
            .Should()
            .Throw<InvalidOperationException>();
    }

    [Theory, AutoNSubstituteData]
    internal void Constructor_Should_Throw_On_Alias_Conflicting_With_Primary_Name(
        IEventDataConverter converter,
        IEventDataConverter otherConverter)
    {
        var typeToNameMappings = new Dictionary<Type, (string Name, IEventDataConverter Converter)>
        {
            { typeof(TestEvent), (nameof(TestEvent), converter) }
        };

        FluentActions
            .Invoking(() => new EventCatalog(
                typeToNameMappings,
                new Dictionary<string, IEventDataConverter>
                {
                    { nameof(TestEvent), otherConverter }
                }))
            .Should()
            .Throw<InvalidOperationException>();
    }

#endif
}