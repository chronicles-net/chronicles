using Chronicles.Documents.DependencyInjection;
using Chronicles.EventStore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Chronicles.EventStore.DependencyInjection;

public static class EventStoreServiceCollectionExtensions
{
    public static EventStoreBuilder WithEventStore(
        this DocumentStoreBuilder documentBuilder,
        Action<EventStoreBuilder> builder)
        => documentBuilder.WithEventStore(_ => { }, builder);

    public static EventStoreBuilder WithEventStore(
        this DocumentStoreBuilder documentBuilder,
        Action<EventStoreOptions> configure,
        Action<EventStoreBuilder> builder)
    {
        var b = new EventStoreBuilder(documentBuilder);
        builder.Invoke(b);
        b.Build();

        documentBuilder.Services.Configure<EventStoreOptions>(
            documentBuilder.StoreName,
            o =>
            {
                o.DocumentStoreName = documentBuilder.StoreName;
                configure.Invoke(o);
            });
        documentBuilder.Services.ConfigureOptions<EventStoreConfigureDocumentStore>();

        documentBuilder.Services.AddSingleton(TimeProvider.System);

        documentBuilder.Services.TryAddSingleton<IEventCatalogFactory, EventCatalogFactory>();
        documentBuilder.Services.TryAddSingleton<IEventDocumentReader, EventDocumentReader>();
        documentBuilder.Services.TryAddSingleton<IEventDocumentWriter, EventDocumentWriter>();
        documentBuilder.Services.TryAddSingleton<IEventDocumentBatchProducer, EventDocumentBatchProducer>();
        documentBuilder.Services.TryAddSingleton<ICheckpointReader, CheckpointReader>();
        documentBuilder.Services.TryAddSingleton<ICheckpointWriter, CheckpointWriter>();
        documentBuilder.Services.TryAddSingleton<IStreamMetadataReader, StreamMetadataReader>();
        documentBuilder.Services.TryAddSingleton<IEventStreamReader, EventStreamReader>();
        documentBuilder.Services.TryAddSingleton<IEventStreamWriter, EventStreamWriter>();

        return b;
    }
}
