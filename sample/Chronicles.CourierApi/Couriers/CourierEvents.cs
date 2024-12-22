using Chronicles.EventStore.DependencyInjection;

namespace Chronicles.CourierApi.Couriers;

public static class CourierEvents
{
    /// <summary>
    /// Registers the courier events with the event store.
    /// </summary>
    /// <param name="builder">Builder to register the events with.</param>
    /// <returns>The <see cref="EventStoreBuilder"/> so that additional calls can be chained.</returns>
    public static EventStoreBuilder AddCourierEvents(
        this EventStoreBuilder builder)
        => builder.AddEvent<CourierRegistered>("courier-registered:v1");

    public record CourierRegistered(
        string FirstName,
        string LastName,
        int MaxNumberOfActiveOrders);
}
