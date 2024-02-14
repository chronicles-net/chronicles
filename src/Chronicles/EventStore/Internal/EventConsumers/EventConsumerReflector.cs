using System.Reflection;

namespace Chronicles.EventStore.Internal.EventConsumers;

public class EventConsumerReflector<T> : IEventConsumerReflector
{
    private readonly Dictionary<Type, MethodInfo> consumeEvents;
    private readonly bool canConsumeAnyEvent;
    private readonly bool canConsumeGroupedEvent;

    public EventConsumerReflector()
    {
        consumeEvents = typeof(T)
            .GetInterfaces()
            .Where(t => t.IsGenericType)
            .Where(t => ImplementsConsumeEventInterfaces(t.GetGenericTypeDefinition()))
            .Select(t => t.GetGenericArguments()[0])
            .Distinct()
            .ToDictionary(
                t => t,
                t => typeof(EventConsumerReflector<T>)
                        .GetRuntimeMethods()
                        .First(m => m.Name.Equals(nameof(ProjectTypedEvent), StringComparison.OrdinalIgnoreCase))
                        .MakeGenericMethod(t));

        canConsumeAnyEvent = typeof(T)
            .GetInterfaces()
            .Any(t => t.UnderlyingSystemType == typeof(IConsumeAnyEvent)
                   || t.UnderlyingSystemType == typeof(IConsumeAnyEventAsync));

        canConsumeGroupedEvent = typeof(T)
            .GetInterfaces()
            .Any(t => t.UnderlyingSystemType == typeof(IConsumeGroupedEvents)
                   || t.UnderlyingSystemType == typeof(IConsumeGroupedEventsAsync));
    }

    public bool CanConsumeOneOrMoreEvents(
        IEnumerable<StreamEvent> events)
        => events.Any(CanConsumeEvent);

    public bool CanConsumeEvent(StreamEvent evt)
        => consumeEvents
            .ContainsKey(evt.Data.GetType())
        || canConsumeAnyEvent
        || canConsumeGroupedEvent;

    public bool IsNotConsumingEvents()
        => consumeEvents.Keys.Count == 0
        && !canConsumeAnyEvent;

    public async ValueTask ConsumeAsync(
        StreamEvent evt,
        object projection,
        CancellationToken cancellationToken)
    {
        if (!CanConsumeEvent(evt))
        {
            return;
        }

        if (canConsumeAnyEvent)
        {
            (projection as IConsumeAnyEvent)?.Consume(evt.Data, evt.Metadata);

            if (projection is IConsumeAnyEventAsync consumeAsync)
            {
                await consumeAsync
                    .ConsumeAsync(evt.Data, evt.Metadata, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        if (consumeEvents.TryGetValue(evt.Data.GetType(), out var method))
        {
            var response = method.Invoke(null, new object[] { projection, evt.Data, evt.Metadata, cancellationToken });

            if (response is ValueTask v)
            {
                await v;
            }
        }
    }

    public async ValueTask ConsumeAsync(
        StreamId streamId,
        StreamEvent[] events,
        object projection,
        CancellationToken cancellationToken)
    {
        if (canConsumeGroupedEvent)
        {
            (projection as IConsumeGroupedEvents)?.Consume(streamId, events);

            if (projection is IConsumeGroupedEventsAsync consumeAsync)
            {
                await consumeAsync
                    .ConsumeAsync(streamId, events, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        foreach (var evt in events)
        {
            await ConsumeAsync(
                    evt,
                    projection,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static bool ImplementsConsumeEventInterfaces(Type genericTypeDefinition)
        => genericTypeDefinition == typeof(IConsumeEvent<>)
        || genericTypeDefinition == typeof(IConsumeEventAsync<>);

    private static async ValueTask ProjectTypedEvent<TEvent>(
        object projection,
        TEvent evt,
        EventMetadata metadata,
        CancellationToken cancellationToken)
        where TEvent : class
    {
        (projection as IConsumeEvent<TEvent>)?.Consume(evt, metadata);

        if (projection is IConsumeEventAsync<TEvent> consumeAsync)
        {
            await consumeAsync
                .ConsumeAsync(evt, metadata, cancellationToken);
        }
    }
}
