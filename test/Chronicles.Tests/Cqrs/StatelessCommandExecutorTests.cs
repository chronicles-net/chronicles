using Chronicles.Cqrs;
using Chronicles.Cqrs.Internal;
using Chronicles.EventStore;

namespace Chronicles.Tests.Cqrs;

public class StatelessCommandExecutorTests
{
    public record TestCommand(string Name);
    public record TestEvent(string Value);

    private static async IAsyncEnumerable<StreamEvent> CreateEventStream(params StreamEvent[] events)
    {
        foreach (var e in events)
            yield return e;
        await Task.CompletedTask;
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Should_Call_Handler_ExecuteAsync(
        TestCommand command,
        StreamMetadata metadata,
        IStateContext stateContext)
    {
        var handler = Substitute.For<IStatelessCommandHandler<TestCommand>>();
        var executor = new StatelessCommandExecutor<TestCommand, IStatelessCommandHandler<TestCommand>>(handler);
        var context = new CommandContext<TestCommand>(command, metadata, stateContext);

        await executor.ExecuteAsync(CreateEventStream(), context, CancellationToken.None);

        await handler.Received(1).ExecuteAsync(context, CancellationToken.None);
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Should_Pass_CancellationToken_To_Handler(
        TestCommand command,
        StreamMetadata metadata,
        IStateContext stateContext)
    {
        var handler = Substitute.For<IStatelessCommandHandler<TestCommand>>();
        var executor = new StatelessCommandExecutor<TestCommand, IStatelessCommandHandler<TestCommand>>(handler);
        var context = new CommandContext<TestCommand>(command, metadata, stateContext);
        using var cts = new CancellationTokenSource();

        await executor.ExecuteAsync(CreateEventStream(), context, cts.Token);

        await handler.Received(1).ExecuteAsync(context, cts.Token);
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Should_Return_Without_Error_When_No_Events(
        TestCommand command,
        StreamMetadata metadata,
        IStateContext stateContext)
    {
        var handler = Substitute.For<IStatelessCommandHandler<TestCommand>>();
        var executor = new StatelessCommandExecutor<TestCommand, IStatelessCommandHandler<TestCommand>>(handler);
        var context = new CommandContext<TestCommand>(command, metadata, stateContext);

        var act = () => executor.ExecuteAsync(CreateEventStream(), context, CancellationToken.None).AsTask();

        await act.Should().NotThrowAsync();
        await handler.Received(1).ExecuteAsync(context, CancellationToken.None);
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Should_Not_Iterate_Events_Stream(
        TestCommand command,
        StreamMetadata metadata,
        IStateContext stateContext)
    {
        var handler = Substitute.For<IStatelessCommandHandler<TestCommand>>();
        var executor = new StatelessCommandExecutor<TestCommand, IStatelessCommandHandler<TestCommand>>(handler);
        var context = new CommandContext<TestCommand>(command, metadata, stateContext);
        var streamIterated = false;

        await executor.ExecuteAsync(TrackingStream(), context, CancellationToken.None);

        streamIterated.Should().BeFalse("stateless handlers must not read the event stream");

        async IAsyncEnumerable<StreamEvent> TrackingStream()
        {
            streamIterated = true;
            yield return new StreamEvent(new TestEvent("ignored"), EventMetadata.Empty);
            await Task.CompletedTask;
        }
    }
}
