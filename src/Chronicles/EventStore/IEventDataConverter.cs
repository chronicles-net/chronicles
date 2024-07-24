namespace Chronicles.EventStore;

public interface IEventDataConverter
{
    object? Convert(
        EventConverterContext context);
}
