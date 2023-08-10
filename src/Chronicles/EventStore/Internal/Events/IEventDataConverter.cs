namespace Chronicles.EventStore.Internal.Events;

public interface IEventDataConverter
{
    object? Convert(
        EventConverterContext context);
}
