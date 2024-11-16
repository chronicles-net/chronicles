using Chronicles.EventStore.DependencyInjection;

namespace Chronicles.CourierApi.Shipments;

public static class ShipmentEvents
{
    public static EventStoreBuilder AddShipmentEvents(
        this EventStoreBuilder builder) => builder
            .AddEvent<ShipmentAssigned>("shipment-assigned:v1")
            .AddEvent<ShipmentCreated>("shipment-created:v1")
            .AddEvent<ShipmentDelivered>("shipment-delivered:v1")
            .AddEvent<ShipmentExpired>("shipment-expired:v1")
            .AddEvent<ShipmentNotAssigned>("shipment-not-assigned:v1");

    public record ShipmentAssigned(
        string CourierId);

    public record ShipmentCreated(
        string CourierId,
        Address Address);

    public record ShipmentDelivered(
        string CourierId);

    public record ShipmentExpired();

    public record ShipmentNotAssigned(
        string CourierId);
}
