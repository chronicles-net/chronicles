namespace Chronicles.EventStore.Internal.Events;

public class StreamEventCatalog : IStreamEventCatalog
{
    private readonly IEventDataConverter[] converters;

    public StreamEventCatalog(
        IEventDataConverter[] converters)
        => this.converters = converters;

    public virtual EventName GetName(Type type)
    {
        foreach (var converter in converters)
        {
            var name = converter.GetName(type);
            if (name != EventName.Unknown)
            {
                return name;
            }
        }

        throw new ArgumentException(
            $"Event of type '{type}' is not registered in stream event catalog.",
            nameof(type));
    }

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

    private object ConvertData(EventConverterContext context)
    {
        foreach (var converter in converters)
        {
            var evt = converter.Convert(context);
            if (evt != null)
            {
                return evt;
            }
        }

        return new UnknownEvent(context.Data.GetRawText());
    }
}
