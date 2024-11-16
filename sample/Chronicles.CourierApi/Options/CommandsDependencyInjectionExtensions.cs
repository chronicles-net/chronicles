using Chronicles.CourierApi.Couriers;
using Chronicles.Cqrs.DependencyInjection;

namespace Chronicles.CourierApi.Shipments;

public static class CommandsDependencyInjectionExtensions
{
    public static CqrsBuilder AddShipmentCommands(
        this CqrsBuilder commands)
        => commands
            .AddCommand<CreateShipment.Command, CreateShipment.Handler>(CreateShipment.Options)
            .AddCommand<AssignShipment.Command, AssignShipment.Handler, ShipmentDocument>()
            .AddCommand<MarkShipmentAsDelivered.Command, MarkShipmentAsDelivered.Handler>();

    public static CqrsBuilder AddCourierCommands(
        this CqrsBuilder commands)
        => commands
            .AddCommand<RegisterCourier.Command, RegisterCourier.Handler>(RegisterCourier.Options);
}