namespace Chronicles.EventStore.Samples;

public static class StartQuest
{
    public record Command(
        string Name,
        string? CorrelationId = null);

    public class Handler
        : ICommandHandler<Command>
    {
        public void Configure(
            StreamMetadata metadata,
            Command command,
            CommandOptions options)
        {
            // Configure command constrains
            options.RequiredState = StreamState.New;
            options.CorrelationId = command.CorrelationId;
        }

        public ValueTask ExecuteAsync(
            ICommandContext<Command> context,
            CancellationToken cancellationToken)
            => context
                .AddEvent(new QuestEvents.QuestStarted(context.Command.Name))
                .AsAsync();
    }
}

public static class JoinQuest
{
    public record Command(
        IReadOnlyCollection<string> Members,
        string? CorrelationId = null);

    // Handler can be registered as a singleton
    public sealed class Handler
        : ICommandHandler<Command>,
        IConsumeEvent<QuestEvents.QuestStarted, Handler.State>
    {
        // Local state object
        public record State(bool IsClosed);

        // Creates initial state before consuming any events.
        public State Create(
            StreamEvent evt)
            => new(false);

        // Consume event with state
        public State Consume(
            QuestEvents.QuestStarted evt,
            EventMetadata metadata,
            State state)
            => state with { IsClosed = false };

        // Configure stream constrains, conflict behaviors and correlation
        public void Configure(
            StreamMetadata metadata,
            Command command,
            CommandOptions options)
        {
            options.RequiredState = StreamState.Active;
            options.CorrelationId = command.CorrelationId;
            options.Behavior = OnConflict.RerunCommand;
            options.BehaviorCount = 3;
        }

        // Execute command
        public ValueTask ExecuteAsync(
            ICommandContext<Command> context,
            CancellationToken cancellationToken)
            => context
                .AddEventWhen(
                    static ctx => ctx.GetState<State>() switch
                    {
                        { } state when state.IsClosed => false,
                        _ => true,
                    },
                    static ctx => new QuestEvents.MembersJoined(ctx.Command.Members))
                .AsAsync();
    }
}