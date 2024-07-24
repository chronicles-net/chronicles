using Chronicles.Cqrs;

namespace Chronicles.EventStore.Samples;

public static class StartQuest
{
    public record Command(
        string Name,
        string? CorrelationId = null);

    public class Handler
        : IStatelessCommandHandler<Command>
    {
        public ValueTask ExecuteAsync(
            Command command,
            ICommandContext<Command> context,
            CancellationToken cancellationToken)
            => context
                .AddEvent(new QuestEvents.QuestStarted(command.Name))
                .AsAsync();
    }
}

public static class JoinQuest
{
    public record Command(
        IReadOnlyCollection<string> Members,
        string? CorrelationId = null);

    // Handler can be registered as a singleton
    public class Handler :
        QuestProjection,
        ICommandHandler<Command, QuestDocument>
    {
        // Execute command
        public ValueTask ExecuteAsync(
            ICommandContext<Command> context,
            QuestDocument state,
            CancellationToken cancellationToken)
            => context
                .AddEventWhen(
                    state,
                    static (ctx, s) => s switch
                    {
                        { } state when state.IsClosed => false,
                        _ => true,
                    },
                    static (ctx, s) => new QuestEvents.MembersJoined(ctx.Command.Members))
                .AsAsync();
    }
}