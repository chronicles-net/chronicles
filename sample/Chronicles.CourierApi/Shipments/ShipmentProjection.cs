using Chronicles.Cqrs;
using Chronicles.EventStore;

namespace Chronicles.CourierApi.Shipments;

public class ShipmentProjection
    : IDocumentProjection<ShipmentDocument>
{
    public ShipmentDocument CreateState(
        StreamId streamId)
        => new(streamId.Id, streamId.Id);

    public ShipmentDocument ConsumeEvent(
        StreamEvent evt,
        ShipmentDocument state)
        => evt.Data switch
        {
            ShipmentEvents.ShipmentAssigned data
                => state with
                {
                    State = ShipmentState.Assigned,
                    CourierId = data.CourierId,
                    ActiveOrders = state.ActiveOrders + 1,
                },

            ShipmentEvents.ShipmentCreated
                => state with
                {
                    State = ShipmentState.Created,
                },

            ShipmentEvents.ShipmentDelivered
                => state with
                {
                    State = ShipmentState.Delivered,
                    ActiveOrders = state.ActiveOrders - 1,
                },

            ShipmentEvents.ShipmentNotAssigned
                => state with
                {
                    State = ShipmentState.Created,
                    CourierId = null,
                },

            _ => state,
        };
}
