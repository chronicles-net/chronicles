namespace Chronicles.EventStore.Internal.Events;

public class StreamEventConverter
{
    private readonly EventRegistry eventRegistry;

    public StreamEventConverter(
        EventRegistry eventRegistry)
        => this.eventRegistry = eventRegistry;

    public virtual EventName GetName(Type type)
        => eventRegistry.GetEventName(type);

    public virtual StreamEvent Convert(
        EventConverterContext context)
    {
        try
        {
            return new(
                ConvertData(context),
                context.Metadata);
        }
        catch (Exception ex)
        {
            return new(
                new FaultedEvent(
                    context.Data.GetRawText(),
                    ex),
                context.Metadata);
        }
    }

    private object ConvertData(
        EventConverterContext context)
        => eventRegistry.GetConverter(context.Metadata.Name)?.Convert(context)
        ?? new UnknownEvent(context.Data.GetRawText());
}