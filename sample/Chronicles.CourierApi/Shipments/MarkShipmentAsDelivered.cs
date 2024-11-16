using Chronicles.Cqrs;

namespace Chronicles.CourierApi.Shipments;

public static class MarkShipmentAsDelivered
{
    public record Command(
        string CourierId);

    public class Handler
        : IStatelessCommandHandler<Command>
    {
        public ValueTask ExecuteAsync(
            ICommandContext<Command> context,
            CancellationToken cancellationToken)
            => context
                .AddEvent(new ShipmentEvents.ShipmentDelivered(
                    context.Command.CourierId))
                .AsAsync();
    }
}