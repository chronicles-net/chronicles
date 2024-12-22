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
    /// An object representing the converted event data. 
    /// Returns <c>null</c> if the conversion is not applicable or fails.
    /// </returns>
    object? Convert(EventConverterContext context);
}
