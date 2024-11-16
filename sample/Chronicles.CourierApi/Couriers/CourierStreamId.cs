using Chronicles.EventStore;

namespace Chronicles.CourierApi.Couriers;

public record CourierStreamId(string Id)
    : StreamId(CategoryName, Id)
{
    public const string CategoryName = "courier";

    public static CourierStreamId Create()
        => new(Guid.NewGuid().ToString());
}
