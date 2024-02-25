using System.Reflection;

namespace Chronicles.EventStore.Internal.EventConsumers;

public class EventConsumerStateReflector<T>
{
    private record NoState();

    private readonly Dictionary<Type, Dictionary<Type, MethodInfo>> consumeEvents;
    private readonly bool canConsumeAnyEvent;
    private readonly bool canConsumeGroupedEvent;

    public EventConsumerStateReflector()
    {
        var method = typeof(EventConsumerStateReflector<T>)
            .GetRuntimeMethods()
            .First(m => m.Name.Equals(nameof(ApplyEventAsync), StringComparison.OrdinalIgnoreCase));

        consumeEvents = typeof(T)
            .GetInterfaces()
            .Where(t => t.IsGenericType)
            .Where(t => ImplementsConsumeEventInterfaces(t.GetGenericTypeDefinition()))
            .Select(t => t.GetGenericArguments())
            .Where(t => t.Length <= 2)
            .Select(t => (evt: t[0], state: t.Skip(1).FirstOrDefault(typeof(NoState))))
            .GroupBy(t => t.evt)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => (x.state, method: method.MakeGenericMethod(x.evt, x.state)))
                      .ToDictionary(x => x.state, x => x.method));

        canConsumeAnyEvent = typeof(T)
            .GetInterfaces()
            .Any(t => t.UnderlyingSystemType == typeof(IConsumeAnyEvent)
                   || t.UnderlyingSystemType == typeof(IConsumeAnyEvent<>)
                   || t.UnderlyingSystemType == typeof(IConsumeAnyEventAsync)
                   || t.UnderlyingSystemType == typeof(IConsumeAnyEventAsync<>));

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
        object instance,
        EventConsumerStateContext context,
        CancellationToken cancellationToken)
    {
        if (!CanConsumeEvent(evt))
        {
            return;
        }

        if (consumeEvents.TryGetValue(evt.Data.GetType(), out var methods))
        {
            foreach (var method in methods)
            {
                var response = method.Value.Invoke(null, [instance, context, evt.Data, evt.Metadata, cancellationToken]);

                if (response is ValueTask v)
                {
                    await v;
                }
            }
        }
    }

    public async ValueTask ConsumeAsync(
        StreamId streamId,
        StreamEvent[] events,
        object instance,
        EventConsumerStateContext stateContext,
        CancellationToken cancellationToken)
    {
        foreach (var evt in events)
        {
            await ConsumeAsync(
                    evt,
                    instance,
                    stateContext,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        if (instance is IConsumeGroupedEvents consume)
        {
            consume.Consume(streamId, events);
        }

        if (instance is IConsumeGroupedEventsAsync consumeAsync)
        {
            await consumeAsync
                .ConsumeAsync(streamId, events, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static bool ImplementsConsumeEventInterfaces(Type genericTypeDefinition)
        => genericTypeDefinition == typeof(IConsumeEvent<>)
        || genericTypeDefinition == typeof(IConsumeEvent<,>)
        || genericTypeDefinition == typeof(IConsumeEventAsync<>)
        || genericTypeDefinition == typeof(IConsumeEventAsync<,>);

    private static async ValueTask ApplyEventAsync<TEvent, TState>(
        object instance,
        EventConsumerStateContext stateContext,
        TEvent evt,
        EventMetadata metadata,
        CancellationToken cancellationToken)
        where TState : class
        where TEvent : class
    {
        var streamEvent = new StreamEvent(evt, metadata);
        var requireState = typeof(TState) != typeof(NoState);
        TState? state = requireState switch
        {
            true => await stateContext
                .GetStateAsync<TState>(streamEvent, cancellationToken)
                .ConfigureAwait(false),
            _ => null,
        };

        if (instance is IConsumeEvent<TEvent> consume)
        {
            consume.Consume(evt, metadata);
        }

        if (instance is IConsumeEvent<TEvent, TState> consumeState && state != null)
        {
            state = consumeState
                .Consume(evt, metadata, state);
        }

        if (instance is IConsumeEventAsync<TEvent> consumeAsync)
        {
            await consumeAsync
                .ConsumeAsync(evt, metadata, cancellationToken)
                .ConfigureAwait(false);
        }

        if (instance is IConsumeEventAsync<TEvent, TState> consumeStateAsync && state != null)
        {
            state = await consumeStateAsync
                .ConsumeAsync(evt, metadata, state, cancellationToken)
                .ConfigureAwait(false);
        }

        if (instance is IConsumeAnyEventAsync consumeAnyAsync)
        {
            await consumeAnyAsync
                .ConsumeAsync(evt, metadata, cancellationToken)
                .ConfigureAwait(false);
        }

        if (instance is IConsumeAnyEventAsync<TState> consumeAnyStateAsync && state != null)
        {
            state = await consumeAnyStateAsync
                .ConsumeAsync(evt, metadata, state, cancellationToken)
                .ConfigureAwait(false);
        }

        if (state != null)
        {
            stateContext.SetState(state);
        }
    }
}