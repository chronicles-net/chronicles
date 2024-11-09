using Chronicles.EventStore;
using Chronicles.EventStore.Internal;
using NSubstitute;

namespace Chronicles.Tests.EventStore.Internal;

public class EventStreamProcessorTests
{
    [Theory, AutoNSubstituteData]
    internal async Task ProcessAsync_Should_Consume_Events(
        IEventProcessor processor,
        StreamEvent[] streamEvents,
        EventMetadata eventMetadata,
        CancellationToken cancellationToken)
    {
        var events = streamEvents
            .Select(e => e with { Metadata = eventMetadata })
            .ToArray();

        var sut = new EventStreamProcessor(eventMetadata.StreamId.Category, [processor]);
        await sut.ProcessAsync(events, cancellationToken);

        foreach (var evt in events)
        {
            await processor
                .Received(1)
                .ConsumeAsync(evt, Arg.Any<IStateContext>(), Arg.Any<bool>(), cancellationToken);
        }
    }

    [Theory, AutoNSubstituteData]
    internal async Task ProcessAsync_Should_Consume_Events_On_Processors(
        IEventProcessor processor1,
        IEventProcessor processor2,
        IEventProcessor processor3,
        StreamEvent[] streamEvents,
        EventMetadata eventMetadata,
        CancellationToken cancellationToken)
    {
        var count = 1;
        var events = streamEvents
            .Select(e => e with { Metadata = eventMetadata with { Version = count++ } })
            .ToArray();

        var sut = new EventStreamProcessor(
            eventMetadata.StreamId.Category,
            [processor1, processor2, processor3]);
        await sut.ProcessAsync(events, cancellationToken);

        count = 0;
        foreach (var evt in events)
        {
            count++;
            await processor1
                .Received(1)
                .ConsumeAsync(evt, Arg.Any<IStateContext>(), count == events.Length, cancellationToken);

            await processor2
                .Received(1)
                .ConsumeAsync(evt, Arg.Any<IStateContext>(), count == events.Length, cancellationToken);

            await processor3
                .Received(1)
                .ConsumeAsync(evt, Arg.Any<IStateContext>(), count == events.Length, cancellationToken);
        }
    }

    [Theory, AutoNSubstituteData]
    internal async Task ProcessAsync_Should_Consume_All_Events_One_Processor_At_A_Time(
        IEventProcessor processor1,
        IEventProcessor processor2,
        IEventProcessor processor3,
        StreamEvent[] streamEvents,
        EventMetadata eventMetadata,
        CancellationToken cancellationToken)
    {
        var count = 1;
        var events = streamEvents
            .Select(e => e with { Metadata = eventMetadata with { Version = count++ } })
            .ToArray();

        IEventProcessor[] processors = [processor1, processor2, processor3];
        var sut = new EventStreamProcessor(
            eventMetadata.StreamId.Category,
            processors);
        await sut.ProcessAsync(events, cancellationToken);

        Received.InOrder(() =>
        {
            foreach (var processor in processors)
            {
                count = 0;
                foreach (var evt in events)
                {
                    count++;
#pragma warning disable CA2012 // Use ValueTasks correctly
                    _ = processor.ConsumeAsync(
                        evt,
                        Arg.Any<IStateContext>(),
                        count == events.Length,
                        cancellationToken);
#pragma warning restore CA2012 // Use ValueTasks correctly
                }
            }
        });
    }
}