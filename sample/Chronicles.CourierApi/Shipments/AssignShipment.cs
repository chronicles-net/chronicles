using Chronicles.Cqrs;

namespace Chronicles.CourierApi.Shipments;

public static class AssignShipment
{
    public record Command(
        string CourierId);

    public class Handler :
        ShipmentProjection,
        ICommandHandler<Command, ShipmentDocument>
    {
        public ValueTask ExecuteAsync(
            ICommandContext<Command> context,
            ShipmentDocument state,
            CancellationToken cancellationToken)
            => context
                .AddEventWhen(
                  given: state,
                  when: static (ctx, doc) => doc.CourierId is null,
                  then: static (ctx, doc) => new ShipmentEvents.ShipmentAssigned(ctx.Command.CourierId))
                .AsAsync();
    }
}