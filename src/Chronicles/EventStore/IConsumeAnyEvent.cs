namespace Chronicles.EventStore;

/// <summary>
/// Implementing this interface will instruct the framework to
/// call <see cref="Consume(object, EventMetadata)"/> for every
/// event in the event stream.
/// </summary>
public interface IConsumeAnyEvent
{
    void Consume(
        object evt,
        EventMetadata metadata);
}
