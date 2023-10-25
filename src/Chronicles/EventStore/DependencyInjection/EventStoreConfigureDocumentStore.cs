using Chronicles.Documents;
using Chronicles.EventStore;
using Chronicles.EventStore.Internal.Checkpoints;
using Chronicles.EventStore.Internal.Converters;
using Chronicles.EventStore.Internal.Events;
using Chronicles.EventStore.Internal.Streams;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Responsible for configuring the underlying <see cref="DocumentOptions"/> with
/// the required containers used by event store.
/// </summary>
public class EventStoreConfigureDocumentStore
    : IConfigureNamedOptions<DocumentOptions>
{
    private readonly IOptionsMonitor<EventStoreOptions> eventStoreOptions;

    public EventStoreConfigureDocumentStore(
        IOptionsMonitor<EventStoreOptions> eventStoreOptions)
        => this.eventStoreOptions = eventStoreOptions;

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
        options.AddDocumentType(typeof(StreamEventDocument), eventStore.EventStoreContainer);
        options.AddDocumentType(typeof(StreamDocument), eventStore.EventStoreContainer);
        options.AddDocumentType(typeof(StreamMetadataDocument), eventStore.EventStoreContainer);
        options.AddDocumentType(typeof(StreamEvent), eventStore.EventStoreContainer);

        var eventRegistry = new EventRegistry(eventStore);
        options.SerializerOptions.Converters.Add(new StreamVersionJsonConverter());
        options.SerializerOptions.Converters.Add(new StreamIdJsonConverter());
        options.SerializerOptions.Converters.Add(new StreamEventDocumentJsonConverter(eventRegistry));
        options.SerializerOptions.Converters.Add(new StreamEventJsonConverter(new StreamEventConverter(eventRegistry)));
    }
}
