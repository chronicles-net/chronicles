using Chronicles.Cqrs;

namespace Chronicles.CourierApi.Shipments;

public static class CreateShipment
{
    public static CommandOptions Options { get; } = new()
    {
        // Ensure that the stream is empty before executing the command
        RequiredState = EventStore.StreamState.New,
    };

    public record Command(
        string CourierId,
        Address Address);

    public class Handler
        : IStatelessCommandHandler<Command>
    {
        public ValueTask ExecuteAsync(
            ICommandContext<Command> context,
            CancellationToken cancellationToken)
            => context
                .AddEvent(new ShipmentEvents.ShipmentCreated(
                    context.Command.CourierId,
                    context.Command.Address))
                .AsAsync();
    }
}
