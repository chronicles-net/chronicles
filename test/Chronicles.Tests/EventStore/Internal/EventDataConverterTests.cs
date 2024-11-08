using System.Text.Json;
using System.Text.Json.Serialization;
using Chronicles.EventStore;
using Chronicles.EventStore.Internal;

namespace Chronicles.Tests.EventStore.Internal;

public class EventDataConverterTests
{
    internal sealed record TestEvent([property: JsonRequired] string NotName);

    [Theory, AutoNSubstituteData]
    internal void Convert_Should_Return_Deserialize_Data(
        TestEvent evt,
        EventMetadata metadata)
    {
        var data = JsonSerializer.SerializeToElement(evt);
        var context = new EventConverterContext(
            data,
            metadata,
            new JsonSerializerOptions());

        new EventDataConverter(metadata.Name, typeof(TestEvent))
            .Convert(context)
            .Should()
            .BeEquivalentTo(evt);
    }

    [Theory, AutoNSubstituteData]
    internal void Convert_Should_Return_Null_When_EventName_Does_Not_Match(
        TestEvent evt,
        EventMetadata metadata)
    {
        var data = JsonSerializer.SerializeToElement(evt);
        var context = new EventConverterContext(
            data,
            metadata,
            new JsonSerializerOptions());

        new EventDataConverter("unknown", typeof(TestEvent))
            .Convert(context)
            .Should()
            .BeNull();
    }

    [Theory, AutoNSubstituteData]
    internal void Convert_Should_Throw_When_Deserialize_Wrong_Type(
        EventMetadata metadata)
    {
        var data = JsonSerializer.SerializeToElement(metadata);
        var context = new EventConverterContext(
            data,
            metadata,
            new JsonSerializerOptions());

        FluentActions
            .Invoking(() => new EventDataConverter(metadata.Name, typeof(TestEvent)).Convert(context))
            .Should()
            .Throw<JsonException>();
    }
}