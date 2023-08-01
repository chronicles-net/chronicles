namespace Chronicles.EventStore.Internal.Events;

public interface IEventDataConverter
{
    EventName GetName(Type type);

    object? Convert(
        EventConverterContext context);
}
