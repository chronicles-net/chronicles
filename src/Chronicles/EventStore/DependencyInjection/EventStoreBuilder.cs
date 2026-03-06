using Chronicles.Documents;
using Chronicles.Documents.DependencyInjection;
// Explicit architectural exception (approved: Lars Skovslund, 2026-03-05):
// EventStore DI wiring is permitted to reference Documents.Internal types to wire
// change-feed subscriptions (IChangeFeedFactory, DocumentSubscription<T,P>,
// IDocumentSubscription). This is a one-way, DI-only coupling — no runtime data
// flows across the boundary. Promoting these types to public would expose
// infrastructure details that are not part of the Documents public API contract.
using Chronicles.Documents.Internal;
using Chronicles.EventStore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Chronicles.EventStore.DependencyInjection;

public class EventStoreBuilder(
    DocumentStoreBuilder documentBuilder)
{
    private readonly Dictionary<Type, (string Name, IEventDataConverter Converter)> eventNames = [];
    private readonly List<(Type EventType, string Alias, IEventDataConverter Converter)> aliasRegistrations = [];
    private readonly EventStoreOptions options = new();

    public IServiceCollection Services => documentBuilder.Services;

    public string StoreName => documentBuilder.StoreName;

    /// <summary>
    ///  Configures the event store to use a specific container name.
    /// </summary>
    /// <param name="containerName">Name of the cosmos container.</param>
    /// <returns>The <see cref="EventStoreBuilder"/> for further configurations.</returns>
    public EventStoreBuilder WithEventStoreContainerName(
        string containerName)
    {
        options.EventStoreContainer = containerName;
        return this;
    }

    /// <summary>
    /// Configures the event store to use a specific container name for stream indexes and checkpoints.
    /// </summary>
    /// <param name="containerName">Name of the container.</param>
    /// <returns>The <see cref="EventStoreBuilder"/> for further configurations.</returns>
    public EventStoreBuilder WithStreamIndexContainerName(
        string containerName)
    {
        options.StreamIndexContainer = containerName;
        return this;
    }

    /// <summary>
    /// Adds an event to the event catalog.
    /// </summary>
    /// <typeparam name="TEvent">Type of event</typeparam>
    /// <param name="name">Name of the event</param>
    /// <returns>The <see cref="EventStoreBuilder"/> for further configurations.</returns>
    public EventStoreBuilder AddEvent<TEvent>(
        string name)
        where TEvent : class
    {
        eventNames[typeof(TEvent)] = (name, new EventDataConverter(name, typeof(TEvent)));

        return this;
    }

    /// <summary>
    /// Adds an event with optional aliases for backwards compatibility.
    /// Aliases are recognized during deserialization but are never used when writing new events.
    /// The primary <paramref name="name"/> is always used for serialization.
    /// </summary>
    /// <typeparam name="TEvent">Type of event.</typeparam>
    /// <param name="name">Primary (canonical) name used when writing new events.</param>
    /// <param name="aliases">
    /// Legacy names recognized during deserialization for backwards compatibility.
    /// Each alias maps to the same event type but is never written.
    /// </param>
    /// <returns>The <see cref="EventStoreBuilder"/> for further configurations.</returns>
    public EventStoreBuilder AddEvent<TEvent>(
        string name,
        params string[] aliases)
        where TEvent : class
    {
        eventNames[typeof(TEvent)] = (name, new EventDataConverter(name, typeof(TEvent)));

        aliasRegistrations.RemoveAll(a => a.EventType == typeof(TEvent));
        foreach (var alias in aliases)
        {
            aliasRegistrations.Add((typeof(TEvent), alias, new EventDataConverter(alias, typeof(TEvent))));
        }

        return this;
    }

    /// <summary>
    /// Adds an event with a custom event converter.
    /// </summary>
    /// <typeparam name="TEvent">Type of event.</typeparam>
    /// <param name="name">Unique name of event.</param>
    /// <param name="customConverter">Custom converter.</param>
    /// <returns>The <see cref="EventStoreBuilder"/> for further configurations.</returns>
    public EventStoreBuilder AddEvent<TEvent>(
        string name,
        IEventDataConverter customConverter)
        where TEvent : class
    {
        Arguments.EnsureNotNull(customConverter, nameof(customConverter));

        eventNames[typeof(TEvent)] = (name, customConverter);

        return this;
    }

    /// <summary>
    ///   Adds a custom event catalog to use for event mapping.
    /// </summary>
    /// <remarks>
    ///   When using a custom event catalog, events mapped using
    ///   <see cref="AddEvent{TEvent}(string)"/>, <see cref="AddEvent{TEvent}(string, string[])"/>,
    ///   or <see cref="AddEvent{TEvent}(string, IEventDataConverter)"/> will not be used.
    /// </remarks>
    /// <typeparam name="TCatalog">Event catalog implementation type.</typeparam>
    /// <returns>The <see cref="EventStoreBuilder"/> for further configurations.</returns>
    public EventStoreBuilder AddEventCatalog<TCatalog>()
        where TCatalog : class, IEventCatalog
    {
        Services.TryAddKeyedSingleton<IEventCatalog, TCatalog>(StoreName);

        return this;
    }

    public EventStoreBuilder AddEventSubscription(
        string name,
        Action<EventSubscriptionBuilder> builder)
        => AddEventSubscription(name, o => { }, builder);

    public EventStoreBuilder AddEventSubscription(
        string name,
        Action<EventSubscriptionOptions> options,
        Action<EventSubscriptionBuilder> builder)
    {
        builder.Invoke(new EventSubscriptionBuilder(name, StoreName, Services));

        Services.ConfigureOptions<ConfigureSubscriptionOptions>();
        Services.Configure<SubscriptionOptions>(name, o => { });
        Services.Configure(name, options);

        // Add default exception handler.
        Services.TryAddKeyedSingleton<IEventSubscriptionExceptionHandler, DefaultEventSubscriptionExceptionHandler>(name);

        Services.AddKeyedSingleton(name, (s, n) =>
            new EventDocumentProcessor(
                s.GetRequiredKeyedService<IEventSubscriptionExceptionHandler>(n),
                s.GetKeyedServices<IEventStreamProcessor>(n)));
        Services.AddSingleton<IDocumentSubscription>(s =>
            new DocumentSubscription<StreamEvent, EventDocumentProcessor>(
                StoreName,
                name,
                s.GetRequiredService<IChangeFeedFactory>(),
                s.GetRequiredKeyedService<EventDocumentProcessor>(name)));

        return this;
    }

    internal void Build()
    {
        ValidateEventNames();

        var aliases = aliasRegistrations.ToDictionary(a => a.Alias, a => a.Converter);
        Services.TryAddKeyedSingleton<IEventCatalog>(StoreName, new EventCatalog(eventNames, aliases));
        Services.Configure<EventStoreOptions>(StoreName, o =>
        {
            o.DocumentStoreName = StoreName;
            o.EventStoreContainer = options.EventStoreContainer;
            o.StreamIndexContainer = options.StreamIndexContainer;
        });
    }

    private void ValidateEventNames()
    {
        var registeredNames = new HashSet<string>();

        foreach (var (_, (name, _)) in eventNames)
        {
            if (!registeredNames.Add(name))
            {
                throw new InvalidOperationException(
                    $"Event name '{name}' is already registered.");
            }
        }

        foreach (var (_, alias, _) in aliasRegistrations)
        {
            if (!registeredNames.Add(alias))
            {
                throw new InvalidOperationException(
                    $"Event name '{alias}' is already registered.");
            }
        }
    }
}
