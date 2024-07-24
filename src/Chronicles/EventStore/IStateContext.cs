namespace Chronicles.EventStore;

/// <summary>
/// Represents a context that can be used to store and retrieve state.
/// </summary>
/// <remarks>
///   Operations on this interface are not thread safe and should be used in a single threaded context.
/// </remarks>
public interface IStateContext
{
    /// <summary>
    /// Gets the state of the specified type.
    /// </summary>
    /// <typeparam name="TState">Type of state data.</typeparam>
    /// <returns>The state found, otherwise <c>null</c> is returned.</returns>
    TState? GetState<TState>()
        where TState : class;

    /// <summary>
    /// Sets the state of the specified type.
    /// </summary>
    /// <typeparam name="TState">Type of state to store.</typeparam>
    /// <param name="state">State to store.</param>
    void SetState<TState>(
        TState state)
        where TState : class;
}