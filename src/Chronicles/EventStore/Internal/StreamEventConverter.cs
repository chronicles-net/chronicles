namespace Chronicles.EventStore.Internal;

internal class StreamEventConverter(
    IEventCatalog eventCatalog)
{
    public virtual EventName GetName(Type type)
        => eventCatalog.GetEventName(type);

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
        => eventCatalog.GetConverter(context.Metadata.Name)?.Convert(context)
        ?? new UnknownEvent(context.Data.GetRawText());
}