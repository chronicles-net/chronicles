using Chronicles.Cqrs;
using Chronicles.Cqrs.Internal;
using Chronicles.EventStore;

namespace Chronicles.Tests.Cqrs;

public class CommandHandlerExecutorTests
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
    public async Task ExecuteAsync_Should_Call_ConsumeEvent_For_Each_Event_In_Stream(
        TestCommand command,
        StreamMetadata metadata,
        IStateContext stateContext)
    {
        var handler = Substitute.For<ICommandHandler<TestCommand>>();
        var executor = new CommandHandlerExecutor<TestCommand, ICommandHandler<TestCommand>>(handler);
        var context = new CommandContext<TestCommand>(command, metadata, stateContext);
        var evt1 = new StreamEvent(new TestEvent("first"), EventMetadata.Empty);
        var evt2 = new StreamEvent(new TestEvent("second"), EventMetadata.Empty);

        await executor.ExecuteAsync(CreateEventStream(evt1, evt2), context, CancellationToken.None);

        handler.Received(1).ConsumeEvent(evt1, command, stateContext);
        handler.Received(1).ConsumeEvent(evt2, command, stateContext);
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Should_Call_Handler_ExecuteAsync_After_Consuming_Events(
        TestCommand command,
        StreamMetadata metadata,
        IStateContext stateContext)
    {
        var handler = Substitute.For<ICommandHandler<TestCommand>>();
        var executor = new CommandHandlerExecutor<TestCommand, ICommandHandler<TestCommand>>(handler);
        var context = new CommandContext<TestCommand>(command, metadata, stateContext);
        var evt = new StreamEvent(new TestEvent("value"), EventMetadata.Empty);
        var callOrder = new List<string>();

        handler
            .When(h => h.ConsumeEvent(Arg.Any<StreamEvent>(), Arg.Any<TestCommand>(), Arg.Any<IStateContext>()))
            .Do(_ => callOrder.Add("consume"));
#pragma warning disable CA2012 // NSubstitute When() lambda discards ValueTask intentionally
        handler
            .When(h => h.ExecuteAsync(Arg.Any<ICommandContext<TestCommand>>(), Arg.Any<CancellationToken>()))
            .Do(_ => callOrder.Add("execute"));
#pragma warning restore CA2012

        await executor.ExecuteAsync(CreateEventStream(evt), context, CancellationToken.None);

        callOrder.Should().ContainInOrder("consume", "execute");
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Should_Pass_CancellationToken_To_Handler(
        TestCommand command,
        StreamMetadata metadata,
        IStateContext stateContext)
    {
        var handler = Substitute.For<ICommandHandler<TestCommand>>();
        var executor = new CommandHandlerExecutor<TestCommand, ICommandHandler<TestCommand>>(handler);
        var context = new CommandContext<TestCommand>(command, metadata, stateContext);
        using var cts = new CancellationTokenSource();

        await executor.ExecuteAsync(CreateEventStream(), context, cts.Token);

        await handler.Received(1).ExecuteAsync(context, cts.Token);
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Should_Not_Call_ConsumeEvent_When_Stream_Is_Empty(
        TestCommand command,
        StreamMetadata metadata,
        IStateContext stateContext)
    {
        var handler = Substitute.For<ICommandHandler<TestCommand>>();
        var executor = new CommandHandlerExecutor<TestCommand, ICommandHandler<TestCommand>>(handler);
        var context = new CommandContext<TestCommand>(command, metadata, stateContext);

        await executor.ExecuteAsync(CreateEventStream(), context, CancellationToken.None);

        handler.DidNotReceive().ConsumeEvent(
            Arg.Any<StreamEvent>(),
            Arg.Any<TestCommand>(),
            Arg.Any<IStateContext>());
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Should_Call_Handler_ExecuteAsync_Even_When_Stream_Is_Empty(
        TestCommand command,
        StreamMetadata metadata,
        IStateContext stateContext)
    {
        var handler = Substitute.For<ICommandHandler<TestCommand>>();
        var executor = new CommandHandlerExecutor<TestCommand, ICommandHandler<TestCommand>>(handler);
        var context = new CommandContext<TestCommand>(command, metadata, stateContext);

        await executor.ExecuteAsync(CreateEventStream(), context, CancellationToken.None);

        await handler.Received(1).ExecuteAsync(context, CancellationToken.None);
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Should_Pass_Command_And_State_From_Context_To_ConsumeEvent(
        TestCommand command,
        StreamMetadata metadata,
        IStateContext stateContext)
    {
        var handler = Substitute.For<ICommandHandler<TestCommand>>();
        var executor = new CommandHandlerExecutor<TestCommand, ICommandHandler<TestCommand>>(handler);
        var context = new CommandContext<TestCommand>(command, metadata, stateContext);
        var evt = new StreamEvent(new TestEvent("value"), EventMetadata.Empty);

        await executor.ExecuteAsync(CreateEventStream(evt), context, CancellationToken.None);

        handler.Received(1).ConsumeEvent(evt, context.Command, context.State);
    }
}
