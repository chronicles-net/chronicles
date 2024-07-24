namespace Chronicles.EventStore;

public class StateContext : IStateContext
{
    private readonly Dictionary<Type, object> states = [];

    public TState? GetState<TState>()
        where TState : class
        => states.ContainsKey(typeof(TState))
         ? states[typeof(TState)] as TState
         : null;

    public void SetState<TState>(TState state)
        where TState : class
        => states[typeof(TState)] = state;
}
