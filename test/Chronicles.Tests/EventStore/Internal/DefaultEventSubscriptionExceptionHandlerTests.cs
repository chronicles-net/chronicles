using Chronicles.EventStore;
using Chronicles.EventStore.Internal;

namespace Chronicles.Tests.EventStore.Internal;

public class DefaultEventSubscriptionExceptionHandlerTests
{
    [Theory, AutoNSubstituteData]
    internal async Task HandleAsync_Should_Return_ValueTask(
        Exception exception,
        DefaultEventSubscriptionExceptionHandler sut)
        => await FluentActions
            .Awaiting(async () => await sut.HandleAsync(exception, null, CancellationToken.None))
            .Should()
            .NotThrowAsync();

    [Theory, AutoNSubstituteData]
    internal async Task HandleAsync_With_StreamEvent_Should_Return_ValueTask(
        Exception exception,
        DefaultEventSubscriptionExceptionHandler sut)
    {
        var streamEvent = new StreamEvent(new { }, EventMetadata.Empty);

        await FluentActions
            .Awaiting(async () => await sut.HandleAsync(exception, streamEvent, CancellationToken.None))
            .Should()
            .NotThrowAsync();
    }
}