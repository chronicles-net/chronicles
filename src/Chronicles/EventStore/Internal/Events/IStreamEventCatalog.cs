namespace Chronicles.EventStore.Internal.Events;

public interface IStreamEventCatalog
{
    StreamEvent Convert(
        EventConverterContext context);

    EventName GetName(Type type);
}