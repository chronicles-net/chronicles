using Chronicles.Documents;
using Chronicles.EventStore;
using Chronicles.EventStore.Internal;
using Chronicles.EventStore.Internal.Converters;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Responsible for configuring the underlying <see cref="DocumentOptions"/> with
/// the required containers used by event store.
/// </summary>
internal class EventStoreConfigureDocumentStore
    : IConfigureNamedOptions<DocumentOptions>
{
    private readonly IEventCatalogFactory eventCatalogFactory;
    private readonly IOptionsMonitor<EventStoreOptions> eventStoreOptions;

    public EventStoreConfigureDocumentStore(
        IEventCatalogFactory eventCatalogFactory,
        IOptionsMonitor<EventStoreOptions> eventStoreOptions)
    {
        this.eventCatalogFactory = eventCatalogFactory;
        this.eventStoreOptions = eventStoreOptions;
    }

    public void Configure(
        DocumentOptions options)
        => Configure(DocumentOptions.DefaultStoreName, options);

    public void Configure(
        string? name,
        DocumentOptions options)
    {
        var eventStore = eventStoreOptions.Get(name);
        if ((name ?? DocumentOptions.DefaultStoreName) != eventStore.DocumentStoreName)
        {
            // We don't have a configuration for an event store so skip setting one up.
            return;
        }

        options.AddDocumentType(typeof(Checkpoint), eventStore.StreamIndexContainer);
        options.AddDocumentType(typeof(CheckpointDocument<>), eventStore.StreamIndexContainer);
        options.AddDocumentType(typeof(EventDocument), eventStore.EventStoreContainer);
        options.AddDocumentType(typeof(EventDocumentBase), eventStore.EventStoreContainer);
        options.AddDocumentType(typeof(StreamMetadataDocument), eventStore.EventStoreContainer);
        options.AddDocumentType(typeof(StreamEvent), eventStore.EventStoreContainer);

        options.SerializerOptions.Converters.Add(new StreamVersionJsonConverter());
        options.SerializerOptions.Converters.Add(new StreamIdJsonConverter());
        options.SerializerOptions.Converters.Add(
            new StreamEventJsonConverter(
                new StreamEventConverter(
                    eventCatalogFactory.Get(name))));
    }
}
