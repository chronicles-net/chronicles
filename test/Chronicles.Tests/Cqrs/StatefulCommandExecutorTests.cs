using Chronicles.Cqrs;
using Chronicles.Cqrs.Internal;
using Chronicles.EventStore;

namespace Chronicles.Tests.Cqrs;

public class StatefulCommandExecutorTests
{
    public record TestCommand(string Name);
    public record TestEvent(string Value);
    public record TestState(int Counter);

    private static async IAsyncEnumerable<StreamEvent> CreateEventStream(params StreamEvent[] events)
    {
        foreach (var e in events)
            yield return e;
        await Task.CompletedTask;
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Should_Create_State_From_Handler(
        StreamId streamId,
        TestCommand command,
        IStateContext stateContext)
    {
        var metadata = Substitute.For<StreamMetadata>(streamId, StreamState.Active, new StreamVersion(1), DateTimeOffset.UtcNow);
        var handler = Substitute.For<ICommandHandler<TestCommand, TestState>>();
        var executor = new StatefulCommandExecutor<TestCommand, ICommandHandler<TestCommand, TestState>, TestState>(handler);
        var context = new CommandContext<TestCommand>(command, metadata, stateContext);
        var initialState = new TestState(0);

        handler.CreateState(streamId).Returns(initialState);

        await executor.ExecuteAsync(CreateEventStream(), context, CancellationToken.None);

        handler.Received(1).CreateState(streamId);
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Should_Call_ConsumeEvent_For_Each_Event(
        StreamId streamId,
        TestCommand command,
        IStateContext stateContext)
    {
        var metadata = Substitute.For<StreamMetadata>(streamId, StreamState.Active, new StreamVersion(2), DateTimeOffset.UtcNow);
        var handler = Substitute.For<ICommandHandler<TestCommand, TestState>>();
        var executor = new StatefulCommandExecutor<TestCommand, ICommandHandler<TestCommand, TestState>, TestState>(handler);
        var context = new CommandContext<TestCommand>(command, metadata, stateContext);
        var initialState = new TestState(0);
        var evt1 = new StreamEvent(new TestEvent("first"), EventMetadata.Empty);
        var evt2 = new StreamEvent(new TestEvent("second"), EventMetadata.Empty);

        handler.CreateState(streamId).Returns(initialState);
        handler.ConsumeEvent(evt1, Arg.Any<TestState>()).Returns(new TestState(1));
        handler.ConsumeEvent(evt2, Arg.Any<TestState>()).Returns(new TestState(2));

        await executor.ExecuteAsync(CreateEventStream(evt1, evt2), context, CancellationToken.None);

        handler.Received(1).ConsumeEvent(evt1, Arg.Any<TestState>());
        handler.Received(1).ConsumeEvent(evt2, Arg.Any<TestState>());
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Should_Pass_Accumulated_State_To_Handler_ExecuteAsync(
        StreamId streamId,
        TestCommand command,
        IStateContext stateContext)
    {
        var metadata = Substitute.For<StreamMetadata>(streamId, StreamState.Active, new StreamVersion(1), DateTimeOffset.UtcNow);
        var handler = Substitute.For<ICommandHandler<TestCommand, TestState>>();
        var executor = new StatefulCommandExecutor<TestCommand, ICommandHandler<TestCommand, TestState>, TestState>(handler);
        var context = new CommandContext<TestCommand>(command, metadata, stateContext);
        var initialState = new TestState(0);
        var stateAfterEvent = new TestState(42);
        var evt = new StreamEvent(new TestEvent("value"), EventMetadata.Empty);

        handler.CreateState(streamId).Returns(initialState);
        handler.ConsumeEvent(evt, initialState).Returns(stateAfterEvent);

        await executor.ExecuteAsync(CreateEventStream(evt), context, CancellationToken.None);

        await handler.Received(1).ExecuteAsync(context, stateAfterEvent, CancellationToken.None);
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Should_Set_State_On_Context(
        StreamId streamId,
        TestCommand command)
    {
        var metadata = Substitute.For<StreamMetadata>(streamId, StreamState.Active, new StreamVersion(1), DateTimeOffset.UtcNow);
        var handler = Substitute.For<ICommandHandler<TestCommand, TestState>>();
        var executor = new StatefulCommandExecutor<TestCommand, ICommandHandler<TestCommand, TestState>, TestState>(handler);
        var stateContext = Substitute.For<IStateContext>();
        var context = new CommandContext<TestCommand>(command, metadata, stateContext);
        var initialState = new TestState(10);

        handler.CreateState(streamId).Returns(initialState);

        await executor.ExecuteAsync(CreateEventStream(), context, CancellationToken.None);

        stateContext.Received(1).SetState(initialState, null);
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Should_Keep_Previous_State_When_ConsumeEvent_Returns_Null(
        StreamId streamId,
        TestCommand command,
        IStateContext stateContext)
    {
        var metadata = Substitute.For<StreamMetadata>(streamId, StreamState.Active, new StreamVersion(1), DateTimeOffset.UtcNow);
        var handler = Substitute.For<ICommandHandler<TestCommand, TestState>>();
        var executor = new StatefulCommandExecutor<TestCommand, ICommandHandler<TestCommand, TestState>, TestState>(handler);
        var context = new CommandContext<TestCommand>(command, metadata, stateContext);
        var initialState = new TestState(5);
        var evt = new StreamEvent(new TestEvent("ignored"), EventMetadata.Empty);

        handler.CreateState(streamId).Returns(initialState);
        handler.ConsumeEvent(evt, initialState).Returns((TestState?)null);

        await executor.ExecuteAsync(CreateEventStream(evt), context, CancellationToken.None);

        // State should remain initialState when ConsumeEvent returns null
        await handler.Received(1).ExecuteAsync(context, initialState, CancellationToken.None);
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Should_Pass_CancellationToken_To_Handler(
        StreamId streamId,
        TestCommand command,
        IStateContext stateContext)
    {
        var metadata = Substitute.For<StreamMetadata>(streamId, StreamState.Active, new StreamVersion(1), DateTimeOffset.UtcNow);
        var handler = Substitute.For<ICommandHandler<TestCommand, TestState>>();
        var executor = new StatefulCommandExecutor<TestCommand, ICommandHandler<TestCommand, TestState>, TestState>(handler);
        var context = new CommandContext<TestCommand>(command, metadata, stateContext);
        var initialState = new TestState(0);
        using var cts = new CancellationTokenSource();

        handler.CreateState(streamId).Returns(initialState);

        await executor.ExecuteAsync(CreateEventStream(), context, cts.Token);

        await handler.Received(1).ExecuteAsync(context, initialState, cts.Token);
    }
}
