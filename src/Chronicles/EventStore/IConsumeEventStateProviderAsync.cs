namespace Chronicles.EventStore;

public interface IConsumeEventStateProviderAsync<TState>
{
    Task<TState> CreateAsync(
        StreamEvent evt,
        CancellationToken cancellationToken);
}