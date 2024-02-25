namespace Chronicles.EventStore;

public static class CommandContextExtensions
{
    public static ValueTask AsAsync<TCommand>(
        this ICommandContext<TCommand> context)
        where TCommand : class
        => default; // default is a completed value task.

    public static ICommandContext<TCommand> AddEventWhen<TCommand>(
        this ICommandContext<TCommand> context,
        Func<ICommandContext<TCommand>, bool> condition,
        Func<ICommandContext<TCommand>, object> eventProvider)
        where TCommand : class
        => condition(context)
         ? context.AddEvent(eventProvider(context))
         : context;

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
            context.SetResponse(respondWith(context, evt));
        }

        return context;
    }
}
