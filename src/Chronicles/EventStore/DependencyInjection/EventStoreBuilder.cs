using Chronicles.Documents;
using Chronicles.Documents.DependencyInjection;
using Chronicles.Documents.Internal;
using Chronicles.EventStore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Chronicles.EventStore.DependencyInjection;

public class EventStoreBuilder(
    DocumentStoreBuilder documentBuilder)
{
    private readonly Dictionary<Type, (string Name, IEventDataConverter Converter)> eventNames = [];
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
    ///   <see cref="AddEvent{TEvent}(string)"/> or <see cref="AddEvent{TEvent}(string, IEventDataConverter)"/> will not be used.
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
        builder.Invoke(new EventSubscriptionBuilder(name, Services));

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
        Services.TryAddKeyedSingleton<IEventCatalog>(StoreName, new EventCatalog(eventNames));
        Services.Configure<EventStoreOptions>(StoreName, o =>
        {
            o.DocumentStoreName = StoreName;
            o.EventStoreContainer = options.EventStoreContainer;
            o.StreamIndexContainer = options.StreamIndexContainer;
        });
    }
}
