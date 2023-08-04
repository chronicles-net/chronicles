using Microsoft.Extensions.DependencyInjection;

namespace Chronicles.EventStore.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static ChroniclesBuilder WithEventStoreDatabase(
        this ChroniclesBuilder builder)
        => builder.WithEventStoreDatabase(o => { });

    public static ChroniclesBuilder WithEventStoreDatabase(
        this ChroniclesBuilder builder,
        Action<EventStoreOptions> configure)
    {
        return builder;
    }
}