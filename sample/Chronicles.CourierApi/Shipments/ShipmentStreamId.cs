using Chronicles.EventStore;

namespace Chronicles.CourierApi.Shipments;

public record ShipmentStreamId(string Id)
    : StreamId(CategoryName, Id)
{
    public const string CategoryName = "shipment";

    public static ShipmentStreamId Create()
        => new(Guid.NewGuid().ToString());
}
