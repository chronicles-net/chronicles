namespace Chronicles.EventStore.Internal;

internal class StateContext : IStateContext
{
    private readonly Dictionary<StateKey, object> states = [];

    private record StateKey(Type Type, string Name);

    public TState? GetState<TState>(string? name = null)
        where TState : class
        => GetState<TState>(
            new StateKey(
                typeof(TState),
                name ?? string.Empty));

    public void SetState<TState>(TState state, string? name = null)
        where TState : class
        => SetState(
            new StateKey(
                typeof(TState),
                name ?? string.Empty),
            state);

    private TState? GetState<TState>(StateKey key)
        where TState : class
        => states.TryGetValue(key, out var value)
         ? value as TState
         : null;

    private void SetState<TState>(StateKey key, TState state)
        where TState : class
        => states[key] = state;
}
