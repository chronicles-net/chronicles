namespace Chronicles.EventStore;

/// <summary>
/// Defines a contract for converting event data within an event store.
/// </summary>
public interface IEventDataConverter
{
    /// <summary>
    /// Converts the event data based on the provided context.
    /// </summary>
    /// <param name="context">The context containing information necessary for the conversion.</param>
    /// <returns>
    /// An object representing the converted event data, or <c>null</c> if this converter
    /// does not handle the event name in the context. Returning <c>null</c> signals that
    /// the event is unrecognized by this converter, causing it to be wrapped as an
    /// <see cref="UnknownEvent"/>.
    /// </returns>
    object? Convert(EventConverterContext context);
}
