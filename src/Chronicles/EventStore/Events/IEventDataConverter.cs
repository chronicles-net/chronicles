namespace Chronicles.EventStore.Events;

public interface IEventDataConverter
{
    object? Convert(
        EventConverterContext context);
}
