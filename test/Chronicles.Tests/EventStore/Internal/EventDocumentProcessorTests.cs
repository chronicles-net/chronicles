using Chronicles.EventStore;
using Chronicles.EventStore.Internal;
using NSubstitute;

namespace Chronicles.Tests.EventStore.Internal;

public class EventDocumentProcessorTests
{
    [Theory, AutoNSubstituteData]
    internal async Task Should_Process_In_Parallel(
        [Frozen(Matching.ImplementedInterfaces)] List<IEventStreamProcessor> processors,
        EventDocumentProcessor sut,
        IReadOnlyCollection<StreamEvent> changes,
        CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource();
        foreach (var processor in processors)
        {
            processor
                .ProcessAsync(default, default)
                .ReturnsForAnyArgs(tcs.Task);
        }

        var task = sut.ProcessAsync(changes, cancellationToken);

        foreach (var processor in processors)
        {
            await processor.WaitForCall(
                p => p.ProcessAsync(changes, cancellationToken));
        }

        task.IsCompleted.Should().BeFalse();

        tcs.SetResult();

        await task;
    }

    [Theory, AutoNSubstituteData]
    internal async Task Should_Call_ExceptionHandler_On_Process_Exception(
        [Frozen] IEventSubscriptionExceptionHandler exceptionHandler,
        [Frozen(Matching.ImplementedInterfaces)] List<IEventStreamProcessor> processors,
        EventDocumentProcessor sut,
        IReadOnlyCollection<StreamEvent> changes,
        List<Exception> exceptions,
        CancellationToken cancellationToken)
    {
        var index = 0;
        foreach (var processor in processors)
        {
            processor
                .ProcessAsync(default, default)
                .ReturnsForAnyArgs(Task.FromException(exceptions[index++]));
        }

        await sut.ProcessAsync(changes, cancellationToken);

        foreach (var exception in exceptions)
        {
            await exceptionHandler
                .Received(1)
                .HandleAsync(exception);
        }
    }
}
