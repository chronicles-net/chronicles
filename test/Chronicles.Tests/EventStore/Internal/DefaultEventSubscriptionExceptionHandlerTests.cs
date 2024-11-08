using Chronicles.EventStore.Internal;

namespace Chronicles.Tests.EventStore.Internal;

public class DefaultEventSubscriptionExceptionHandlerTests
{
    [Theory, AutoNSubstituteData]
    internal async Task HandleAsync_Should_Return_ValueTask(
        Exception exception,
        DefaultEventSubscriptionExceptionHandler sut)
        => await FluentActions
            .Awaiting(async () => await sut.HandleAsync(exception))
            .Should()
            .NotThrowAsync();
}