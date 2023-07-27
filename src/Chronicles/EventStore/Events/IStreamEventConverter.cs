namespace Chronicles.EventStore.Events;

public interface IStreamEventConverter
{
    StreamEvent Convert(
        EventConverterContext context);
}