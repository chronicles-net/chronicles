using Chronicles;
using Chronicles.EventStore;
using Chronicles.EventStore.Internal.Checkpoints;
using Chronicles.EventStore.Internal.Commands;
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
        builder.Services.TryAddSingleton<IStreamEventReader, StreamEventReader>();
        builder.Services.TryAddSingleton<IStreamEventWriter, StreamEventWriter>();
        builder.Services.TryAddSingleton<CheckpointReader>();
        builder.Services.TryAddSingleton<CheckpointWriter>();
        builder.Services.TryAddSingleton<IStreamMetadataReader, StreamMetadataReader>();
        builder.Services.TryAddSingleton<EventDocumentBatchProducer>();
        builder.Services.TryAddSingleton<IEventConsumerFactory, EventConsumerFactory>();
        builder.Services.TryAddSingleton<ICommandHandlerFactory, CommandHandlerFactory>();
        builder.Services.TryAddSingleton(typeof(ICommandProcessor<>), typeof(CommandProcessor<>));
        builder.Services.TryAddSingleton(typeof(EventConsumerReflector<>));
        builder.Services.TryAddSingleton(typeof(EventConsumerStateReflector<>));

        builder.Services.AddSingleton<IEventStoreClient, EventStoreClient>();

        return b;
    }
}