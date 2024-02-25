namespace Chronicles.EventStore;

public interface IConsumeEventAsync<in TEvent>
{
    Task ConsumeAsync(
        TEvent evt,
        EventMetadata metadata,
        CancellationToken cancellationToken);
}

public interface IConsumeEventAsync<in TEvent, TState>
    : IConsumeEventStateProviderAsync<TState>
{
    Task<TState> ConsumeAsync(
        TEvent evt,
        EventMetadata metadata,
        TState state,
        CancellationToken cancellationToken);
}