using Chronicles.Cqrs;
using Chronicles.EventStore;

namespace Chronicles.CourierApi.Couriers;

public class CourierProjection
    : IDocumentProjection<CourierDocument>
{
    public CourierDocument CreateState(
        StreamId streamId)
        => new(streamId.Id, streamId.Id);

    public CourierDocument ConsumeEvent(
        StreamEvent evt,
        CourierDocument state)
        => evt.Data switch
        {
            CourierEvents.CourierRegistered data
                => state with
                {
                    FirstName = data.FirstName,
                    LastName = data.LastName,
                },

            _ => state,
        };
}
