namespace Chronicles.EventStore.Internal.EventConsumers;

public class EventConsumerStateContext(
    object instance)
{
    private readonly Dictionary<Type, object> states = [];

    public TState? GetState<TState>()
        where TState : class
        => states.ContainsKey(typeof(TState))
         ? states[typeof(TState)] as TState
         : null;

    public async ValueTask<TState> GetStateAsync<TState>(
        StreamEvent evt,
        CancellationToken cancellationToken)
        where TState : class
    {
        if (!states.ContainsKey(typeof(TState)))
        {
            if (instance is IConsumeEventStateProvider<TState> provider)
            {
                states[typeof(TState)] = provider.Create(evt);
            }

            if (instance is IConsumeEventStateProviderAsync<TState> providerAsync)
            {
                states[typeof(TState)] = await providerAsync.CreateAsync(evt, cancellationToken);
            }
        }

        return (TState)states[typeof(TState)];
    }

    public void SetState<TState>(
        TState state)
        where TState : class
    {
        states[typeof(TState)] = state;
    }
}
