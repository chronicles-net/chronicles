using Chronicles.Cqrs;

namespace Chronicles.CourierApi.Couriers;

public static class RegisterCourier
{
    public static readonly CommandOptions Options = new()
    {
        RequiredState = EventStore.StreamState.New,
    };

    public record Command(
        string FirstName,
        string LastName,
        int MaxNumberOfActiveOrders);

    public class Handler
        : IStatelessCommandHandler<Command>
    {
        public ValueTask ExecuteAsync(
            ICommandContext<Command> context,
            CancellationToken cancellationToken)
            => context
                .AddEvent(new CourierEvents.CourierRegistered(
                    context.Command.FirstName,
                    context.Command.LastName,
                    context.Command.MaxNumberOfActiveOrders))
                .AsAsync();
    }
}
