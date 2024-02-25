namespace Chronicles.EventStore;

/// <summary>
/// Implementing this interface will instruct the framework to
/// call <see cref="ConsumeAsync(object, EventMetadata, CancellationToken)"/> for every
/// event in the event stream.
/// </summary>
public interface IConsumeAnyEventAsync
{
    Task ConsumeAsync(
        object evt,
        EventMetadata metadata,
        CancellationToken cancellationToken);
}

public interface IConsumeAnyEventAsync<TState>
    : IConsumeEventStateProviderAsync<TState>
{
    Task<TState> ConsumeAsync(
        object evt,
        EventMetadata metadata,
        TState state,
        CancellationToken cancellationToken);
}