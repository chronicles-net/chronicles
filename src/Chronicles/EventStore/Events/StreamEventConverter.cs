namespace Chronicles.EventStore.Events;

public class StreamEventConverter : IStreamEventConverter
{
    private readonly IEventDataConverter[] converters;

    public StreamEventConverter(
        IEventDataConverter[] converters)
        => this.converters = converters;

    public StreamEvent Convert(
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
