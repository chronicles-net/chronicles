using Chronicles.EventStore.DependencyInjection;

namespace Chronicles.CourierApi.Couriers;

public static class CourierEvents
{
    public static EventStoreBuilder AddCourierEvents(
        this EventStoreBuilder builder)
        => builder.AddEvent<CourierRegistered>("courier-registered:v1");

    public record CourierRegistered(
        string FirstName,
        string LastName,
        int MaxNumberOfActiveOrders);
}
