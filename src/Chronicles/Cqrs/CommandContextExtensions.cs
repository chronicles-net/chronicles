namespace Chronicles.Cqrs;

/// <summary>
/// Represents extension methods for <see cref="ICommandContext{TCommand}"/>
/// to allow a builder pattern approach to adding events and response.
/// </summary>
public static class CommandContextExtensions
{
    /// <summary>
    /// Converts the <see cref="ICommandContext{TCommand}"/> to an asynchronous operation.
    /// </summary>
    /// <typeparam name="TCommand">Command type.</typeparam>
    /// <param name="context">Command context.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    public static ValueTask AsAsync<TCommand>(
        this ICommandContext<TCommand> context)
        where TCommand : class
        => ValueTask.CompletedTask;

    /// <summary>
    /// Conditionally adds an event to the event stream.
    /// </summary>
    /// <typeparam name="TCommand">Command type.</typeparam>
    /// <param name="context">Command context.</param>
    /// <param name="condition">Delegate determining if an event should be added.</param>
    /// <param name="addEvent">Delegate for adding an event to the <paramref name="context"/> when <paramref name="condition"/> is <c>true</c>.</param>
    /// <returns>A <see cref="ICommandContext{TCommand}"/> that can be used for further command processing.</returns>
    public static ICommandContext<TCommand> AddEventWhen<TCommand>(
        this ICommandContext<TCommand> context,
        Func<ICommandContext<TCommand>, bool> condition,
        Func<ICommandContext<TCommand>, object> addEvent)
        where TCommand : class
        => condition(context)
         ? context.AddEvent(addEvent(context))
         : context;

    /// <summary>
    /// Conditionally adds an event to the event stream.
    /// </summary>
    /// <typeparam name="TCommand">Command type.</typeparam>
    /// <typeparam name="TState">State type.</typeparam>
    /// <param name="context">Command context.</param>
    /// <param name="state">State object.</param>
    /// <param name="condition">Delegate determining if an event should be added.</param>
    /// <param name="addEvent">Delegate for adding an event to the <paramref name="context"/> when <paramref name="condition"/> is <c>true</c>.</param>
    /// <returns>A <see cref="ICommandContext{TCommand}"/> that can be used for further command processing.</returns>
    public static ICommandContext<TCommand> AddEventWhen<TCommand, TState>(
        this ICommandContext<TCommand> context,
        TState state,
        Func<ICommandContext<TCommand>, TState, bool> condition,
        Func<ICommandContext<TCommand>, TState, object> addEvent)
        where TCommand : class
        where TState : class
        => condition(context, state)
         ? context.AddEvent(addEvent(context, state))
         : context;

    /// <summary>
    /// Conditionally adds an event to the event stream and responds with a value.
    /// </summary>
    /// <typeparam name="TCommand">Command type</typeparam>
    /// <typeparam name="TResponse">Type of response.</typeparam>
    /// <param name="context">Command context.</param>
    /// <param name="condition">Delegate determining if an event should be added.</param>
    /// <param name="addEvent">Delegate for adding an event to the <paramref name="context"/> when <paramref name="condition"/> is <c>true</c>.</param>
    /// <param name="respondWith">Delegate for providing a <seealso cref="ICommandContext{TCommand}.Response"/>.</param>
    /// <returns>A <see cref="ICommandContext{TCommand}"/> that can be used for further command processing.</returns>
    public static ICommandContext<TCommand> AddEventWhen<TCommand, TResponse>(
        this ICommandContext<TCommand> context,
        Func<ICommandContext<TCommand>, bool> condition,
        Func<ICommandContext<TCommand>, object> addEvent,
        Func<ICommandContext<TCommand>, object, TResponse> respondWith)
        where TCommand : class
        where TResponse : class
    {
        if (condition(context))
        {
            var evt = addEvent(context);
            context.AddEvent(addEvent(context));
            context.Response = respondWith(context, evt);
        }

        return context;
    }

    /// <summary>
    /// Conditionally adds an event to the event stream and responds with a value.
    /// </summary>
    /// <typeparam name="TCommand">Command type</typeparam>
    /// <typeparam name="TState">State type.</typeparam>
    /// <typeparam name="TResponse">Type of response.</typeparam>
    /// <param name="context">Command context.</param>
    /// <param name="state">State object.</param>
    /// <param name="condition">Delegate determining if an event should be added.</param>
    /// <param name="addEvent">Delegate for adding an event to the <paramref name="context"/> when <paramref name="condition"/> is <c>true</c>.</param>
    /// <param name="respondWith">Delegate for providing a <seealso cref="ICommandContext{TCommand}.Response"/>.</param>
    /// <returns>A <see cref="ICommandContext{TCommand}"/> that can be used for further command processing.</returns>
    public static ICommandContext<TCommand> AddEventWhen<TCommand, TState, TResponse>(
        this ICommandContext<TCommand> context,
        TState state,
        Func<ICommandContext<TCommand>, TState, bool> condition,
        Func<ICommandContext<TCommand>, TState, object> addEvent,
        Func<ICommandContext<TCommand>, TState, object, TResponse> respondWith)
        where TCommand : class
        where TState : class
        where TResponse : class
    {
        if (condition(context, state))
        {
            var evt = addEvent(context, state);
            context.AddEvent(addEvent(context, state));
            context.Response = respondWith(context, state, evt);
        }

        return context;
    }
}
