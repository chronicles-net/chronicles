using Chronicles.EventStore;

namespace Chronicles.Tests.EventStore;

public class EventMetadataTests
{
    [Fact]
    public void Empty_Has_Null_EventId()
        => EventMetadata.Empty.EventId.Should().BeNull();

    [Fact]
    public void EventId_Can_Be_Set_Using_With_Syntax()
    {
        var metadata = EventMetadata.Empty with { EventId = "test-event-id" };

        metadata.EventId.Should().Be("test-event-id");
    }

    [Fact]
    public void EventId_Is_Preserved_Through_Record_Copy()
    {
        var original = EventMetadata.Empty with { EventId = "original-id" };
        var copy = original with { };

        copy.EventId.Should().Be("original-id");
    }
}
