using System.Net;
using Chronicles.EventStore;
using Chronicles.EventStore.Internal;
using Microsoft.Azure.Cosmos;
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
            await processor.WaitForReceived(
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
                .HandleAsync(exception, Arg.Any<StreamEvent?>(), Arg.Any<CancellationToken>());
        }
    }

    [Theory, AutoNSubstituteData]
    internal async Task Should_Call_ExceptionHandler_On_Error(
        [Frozen] IEventSubscriptionExceptionHandler exceptionHandler,
        EventDocumentProcessor sut,
        string leaseToken,
        Exception exception)
    {
        await sut.ErrorAsync(leaseToken, exception);

        await exceptionHandler
            .Received(1)
            .HandleAsync(exception, null, CancellationToken.None);
    }

    [Theory, AutoNSubstituteData]
    internal async Task Should_Call_ExceptionHandler_On_Non_Lease_CosmosException(
        [Frozen] IEventSubscriptionExceptionHandler exceptionHandler,
        EventDocumentProcessor sut,
        string leaseToken)
    {
        var exception = new CosmosException("boom", HttpStatusCode.ServiceUnavailable, 0, "activity", 0);

        await sut.ErrorAsync(leaseToken, exception);

        await exceptionHandler
            .Received(1)
            .HandleAsync(exception, null, CancellationToken.None);
    }

    [Theory, AutoNSubstituteData]
    internal async Task Should_Ignore_Lease_Taken_Over_Error(
        [Frozen] IEventSubscriptionExceptionHandler exceptionHandler,
        EventDocumentProcessor sut,
        string leaseToken)
    {
        var exception = new CosmosException(
            "The lease token was taken over by owner 'other'",
            HttpStatusCode.PreconditionFailed,
            0,
            "activity",
            0);

        await sut.ErrorAsync(leaseToken, exception);

        await exceptionHandler
            .DidNotReceiveWithAnyArgs()
            .HandleAsync(default!, default, default);
    }

    [Theory, AutoNSubstituteData]
    internal async Task Should_Ignore_Wrapped_Lease_Lost_Error(
        [Frozen] IEventSubscriptionExceptionHandler exceptionHandler,
        EventDocumentProcessor sut,
        string leaseToken)
    {
        var exception = new InvalidOperationException(
            "outer",
            new LeaseLostException("Lease was lost due to load balancing"));

        await sut.ErrorAsync(leaseToken, exception);

        await exceptionHandler
            .DidNotReceiveWithAnyArgs()
            .HandleAsync(default!, default, default);
    }

    [Theory, AutoNSubstituteData]
    internal async Task Should_Call_ExceptionHandler_On_User_Exception_With_PreconditionFailed(
        [Frozen] IEventSubscriptionExceptionHandler exceptionHandler,
        EventDocumentProcessor sut,
        string leaseToken,
        ChangeFeedProcessorContext context)
    {
        var exception = new ChangeFeedProcessorUserException(
            new CosmosException("conflict", HttpStatusCode.PreconditionFailed, 0, "activity", 0),
            context);

        await sut.ErrorAsync(leaseToken, exception);

        await exceptionHandler
            .Received(1)
            .HandleAsync(exception, null, CancellationToken.None);
    }

    private sealed class LeaseLostException(string message)
        : Exception(message);
}
