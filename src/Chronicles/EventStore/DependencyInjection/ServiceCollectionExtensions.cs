using Chronicles;
using Chronicles.EventStore;
using Chronicles.EventStore.Internal.Checkpoints;
using Chronicles.EventStore.Internal.EventConsumers;
using Chronicles.EventStore.Internal.Events;
using Chronicles.EventStore.Internal.Streams;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class EventStoreServiceCollectionExtensions
{
    public static EventStoreBuilder AddEventStore(
        this DocumentStoreBuilder builder,
        Action<EventStoreBuilder> configure)
    {
        var b = new EventStoreBuilder(builder);
        configure.Invoke(b);

        builder.Services.TryAddSingleton<IDateTimeProvider, UtcDateTimeProvider>();
        builder.Services.TryAddSingleton<StreamEventReader>();
        builder.Services.TryAddSingleton<StreamEventWriter>();
        builder.Services.TryAddSingleton<CheckpointReader>();
        builder.Services.TryAddSingleton<CheckpointWriter>();
        builder.Services.TryAddSingleton<StreamMetadataReader>();
        builder.Services.TryAddSingleton<EventDocumentBatchProducer>();
        builder.Services.TryAddSingleton<IEventConsumerFactory, EventConsumerFactory>();
        builder.Services.TryAddSingleton(typeof(EventConsumerReflector<>));

        builder.Services.AddSingleton<IEventStoreClient, EventStoreClient>();

        return b;
    }
}