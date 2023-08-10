using Chronicles;
using Chronicles.EventStore;
using Chronicles.EventStore.Internal.Checkpoints;
using Chronicles.EventStore.Internal.Events;
using Chronicles.EventStore.Internal.Streams;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class EventStoreServiceCollectionExtensions
{
    public static ChroniclesBuilder AddEventStore(
        this ChroniclesBuilder builder,
        string? storeName = default)
        => builder.AddEventStore(storeName, o => { });

    public static ChroniclesBuilder AddEventStore(
        this ChroniclesBuilder builder,
        Action<EventStoreOptions> configure)
        => builder.AddEventStore(
            storeName: Options.Options.DefaultName,
            configure: configure);

    public static ChroniclesBuilder AddEventStore(
        this ChroniclesBuilder builder,
        string? storeName,
        Action<EventStoreOptions> configure)
    {
        if (builder.Services.Any(sd => sd.ServiceType == typeof(StreamEventReader)))
        {
            throw new InvalidOperationException(); // Not allowed to register event store more than once.
        }

        builder.Services.Configure<EventStoreOptions>(o =>
        {
            o.DocumentStoreName = storeName ?? Options.Options.DefaultName;
            configure.Invoke(o);
        });
        builder.Services.ConfigureOptions<EventStoreConfigureDocumentStore>();

        builder.Services.TryAddSingleton<IDateTimeProvider, UtcDateTimeProvider>();
        builder.Services.TryAddSingleton<StreamEventReader>();
        builder.Services.TryAddSingleton<StreamEventWriter>();
        builder.Services.TryAddSingleton<CheckpointReader>();
        builder.Services.TryAddSingleton<CheckpointWriter>();
        builder.Services.TryAddSingleton<StreamMetadataReader>();
        builder.Services.TryAddSingleton<EventDocumentBatchProducer>();

        builder.Services.AddSingleton<IEventStoreClient, EventStoreClient>();

        return builder;
    }
}
