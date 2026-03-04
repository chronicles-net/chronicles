using Chronicles.Cqrs;
using Chronicles.Cqrs.Internal;
using Chronicles.EventStore;
using NSubstitute;

namespace Chronicles.Tests.Cqrs;

public class CommandContextExtensionsTests
{
    public record TestCommand(string Name);
    public record TestEvent(string Value);
    public record TestResponse(string Message);
    public record TestState(int Counter);

    [Theory, AutoNSubstituteData]
    public void AsAsync_Should_Return_Completed_Task(
        ICommandContext<TestCommand> context)
    {
        var result = context.AsAsync();

        result.IsCompleted.Should().BeTrue();
    }

    [Theory, AutoNSubstituteData]
    public void AddEventWhen_Should_Add_Event_When_Condition_Is_True(
        TestCommand command,
        StreamMetadata metadata,
        IStateContext stateContext)
    {
        var context = new CommandContext<TestCommand>(command, metadata, stateContext);
        var evt = new TestEvent("test-value");

        var result = context.AddEventWhen(
            ctx => true,
            ctx => evt);

        context.Events.Should().ContainSingle();
        context.Events.Should().Contain(evt);
        result.Should().BeSameAs(context);
    }

    [Theory, AutoNSubstituteData]
    public void AddEventWhen_Should_Not_Add_Event_When_Condition_Is_False(
        TestCommand command,
        StreamMetadata metadata,
        IStateContext stateContext)
    {
        var context = new CommandContext<TestCommand>(command, metadata, stateContext);

        var result = context.AddEventWhen(
            ctx => false,
            ctx => new TestEvent("test-value"));

        context.Events.Should().BeEmpty();
        result.Should().BeSameAs(context);
    }

    [Theory, AutoNSubstituteData]
    public void AddEventWhen_Should_Not_Call_AddEvent_Delegate_When_Condition_Is_False(
        TestCommand command,
        StreamMetadata metadata,
        IStateContext stateContext)
    {
        var context = new CommandContext<TestCommand>(command, metadata, stateContext);
        var addEventCalled = false;

        context.AddEventWhen(
            ctx => false,
            ctx =>
            {
                addEventCalled = true;
                return new TestEvent("test-value");
            });

        addEventCalled.Should().BeFalse();
    }

    [Theory, AutoNSubstituteData]
    public void AddEventWhen_With_State_Should_Add_Event_When_Condition_Is_True(
        TestCommand command,
        StreamMetadata metadata,
        IStateContext stateContext)
    {
        var context = new CommandContext<TestCommand>(command, metadata, stateContext);
        var state = new TestState(42);
        var evt = new TestEvent("state-value");

        var result = context.AddEventWhen(
            state,
            (ctx, st) => st.Counter == 42,
            (ctx, st) => evt);

        context.Events.Should().ContainSingle();
        context.Events.Should().Contain(evt);
        result.Should().BeSameAs(context);
    }

    [Theory, AutoNSubstituteData]
    public void AddEventWhen_With_State_Should_Not_Add_Event_When_Condition_Is_False(
        TestCommand command,
        StreamMetadata metadata,
        IStateContext stateContext)
    {
        var context = new CommandContext<TestCommand>(command, metadata, stateContext);
        var state = new TestState(10);

        var result = context.AddEventWhen(
            state,
            (ctx, st) => st.Counter == 42,
            (ctx, st) => new TestEvent("state-value"));

        context.Events.Should().BeEmpty();
        result.Should().BeSameAs(context);
    }

    [Theory, AutoNSubstituteData]
    public void AddEventWhen_With_Response_Should_Add_Event_And_Set_Response_When_Condition_Is_True(
        TestCommand command,
        StreamMetadata metadata,
        IStateContext stateContext)
    {
        var context = new CommandContext<TestCommand>(command, metadata, stateContext);
        var evt = new TestEvent("test-value");
        var response = new TestResponse("success");

        var result = context.AddEventWhen(
            ctx => true,
            ctx => evt,
            (ctx, e) => response);

        context.Events.Should().HaveCount(1);
        context.Response.Should().Be(response);
        result.Should().BeSameAs(context);
    }

    [Theory, AutoNSubstituteData]
    public void AddEventWhen_With_Response_Should_Not_Add_Event_Or_Set_Response_When_Condition_Is_False(
        TestCommand command,
        StreamMetadata metadata,
        IStateContext stateContext)
    {
        var context = new CommandContext<TestCommand>(command, metadata, stateContext);

        var result = context.AddEventWhen<TestCommand, TestResponse>(
            ctx => false,
            ctx => new TestEvent("test-value"),
            (ctx, e) => new TestResponse("success"));

        context.Events.Should().BeEmpty();
        context.Response.Should().BeNull();
        result.Should().BeSameAs(context);
    }

    [Theory, AutoNSubstituteData]
    public void AddEventWhen_With_Response_Should_Add_Event_And_RespondWith_Same_Instance(
        TestCommand command,
        StreamMetadata metadata,
        IStateContext stateContext)
    {
        var context = new CommandContext<TestCommand>(command, metadata, stateContext);
        var callCount = 0;
        object? respondWithReceivedEvent = null;

        context.AddEventWhen(
            ctx => true,
            ctx =>
            {
                callCount++;
                return new TestEvent($"call-{callCount}");
            },
            (ctx, e) =>
            {
                respondWithReceivedEvent = e;
                return new TestResponse("success");
            });

        callCount.Should().Be(1);
        context.Events.Should().HaveCount(1);
        respondWithReceivedEvent.Should().BeSameAs(context.Events.Single());
    }

    [Theory, AutoNSubstituteData]
    public void AddEventWhen_With_State_And_Response_Should_Add_Event_And_Set_Response_When_Condition_Is_True(
        TestCommand command,
        StreamMetadata metadata,
        IStateContext stateContext)
    {
        var context = new CommandContext<TestCommand>(command, metadata, stateContext);
        var state = new TestState(42);
        var evt = new TestEvent("state-value");
        var response = new TestResponse("success");

        var result = context.AddEventWhen(
            state,
            (ctx, st) => st.Counter == 42,
            (ctx, st) => evt,
            (ctx, st, e) => response);

        context.Events.Should().HaveCount(1);
        context.Response.Should().Be(response);
        result.Should().BeSameAs(context);
    }

    [Theory, AutoNSubstituteData]
    public void AddEventWhen_With_State_And_Response_Should_Not_Add_Event_When_Condition_Is_False(
        TestCommand command,
        StreamMetadata metadata,
        IStateContext stateContext)
    {
        var context = new CommandContext<TestCommand>(command, metadata, stateContext);
        var state = new TestState(10);

        var result = context.AddEventWhen<TestCommand, TestState, TestResponse>(
            state,
            (ctx, st) => st.Counter == 42,
            (ctx, st) => new TestEvent("state-value"),
            (ctx, st, e) => new TestResponse("success"));

        context.Events.Should().BeEmpty();
        context.Response.Should().BeNull();
        result.Should().BeSameAs(context);
    }

    [Theory, AutoNSubstituteData]
    public void AddEventWhen_With_State_And_Response_Should_Add_Event_And_RespondWith_Same_Instance(
        TestCommand command,
        StreamMetadata metadata,
        IStateContext stateContext)
    {
        var context = new CommandContext<TestCommand>(command, metadata, stateContext);
        var state = new TestState(42);
        var callCount = 0;
        object? respondWithReceivedEvent = null;

        context.AddEventWhen(
            state,
            (ctx, st) => st.Counter == 42,
            (ctx, st) =>
            {
                callCount++;
                return new TestEvent($"call-{callCount}");
            },
            (ctx, st, e) =>
            {
                respondWithReceivedEvent = e;
                return new TestResponse("success");
            });

        callCount.Should().Be(1);
        context.Events.Should().HaveCount(1);
        respondWithReceivedEvent.Should().BeSameAs(context.Events.Single());
    }

    [Theory, AutoNSubstituteData]
    public async Task WithStateResponse_Should_Build_State_And_Set_As_Response_On_Completed(
        TestCommand command,
        StreamId streamId,
        IStateContext stateContext)
    {
        var metadata = Substitute.For<StreamMetadata>(streamId, StreamState.Active, new StreamVersion(5), DateTimeOffset.UtcNow);
        var context = new CommandContext<TestCommand>(command, metadata, stateContext);
        var projection = Substitute.For<IStateProjection<TestState>>();
        var initialState = new TestState(0);
        var evt1 = new StreamEvent(new TestEvent("event1"), EventMetadata.Empty);
        var evt2 = new StreamEvent(new TestEvent("event2"), EventMetadata.Empty);

        projection.CreateState(streamId).Returns(initialState);
        projection.ConsumeEvent(evt1, Arg.Any<TestState>()).Returns(new TestState(1));
        projection.ConsumeEvent(evt2, Arg.Any<TestState>()).Returns(new TestState(2));

        stateContext.GetState<TestState>().Returns((TestState?)null);

        context.WithStateResponse(projection);

        var completionContext = Substitute.For<ICommandCompletionContext<TestCommand>>();
        completionContext.Events.Returns([evt1, evt2]);
        completionContext.State.Returns(stateContext);

        await context.OnCompleteAsync(completionContext, CancellationToken.None);

        completionContext.Response.Should().BeOfType<TestState>();
        ((TestState)completionContext.Response!).Counter.Should().Be(2);
    }

    [Theory, AutoNSubstituteData]
    public async Task WithStateResponse_Should_Use_Existing_State_If_Available(
        TestCommand command,
        StreamId streamId,
        IStateContext stateContext)
    {
        var metadata = Substitute.For<StreamMetadata>(streamId, StreamState.Active, new StreamVersion(5), DateTimeOffset.UtcNow);
        var context = new CommandContext<TestCommand>(command, metadata, stateContext);
        var projection = Substitute.For<IStateProjection<TestState>>();
        var existingState = new TestState(100);
        var evt1 = new StreamEvent(new TestEvent("event1"), EventMetadata.Empty);

        projection.ConsumeEvent(evt1, Arg.Any<TestState>()).Returns(new TestState(101));
        stateContext.GetState<TestState>().Returns(existingState);

        context.WithStateResponse(projection);

        var completionContext = Substitute.For<ICommandCompletionContext<TestCommand>>();
        completionContext.Events.Returns([evt1]);
        completionContext.State.Returns(stateContext);

        await context.OnCompleteAsync(completionContext, CancellationToken.None);

        projection.DidNotReceive().CreateState(Arg.Any<StreamId>());
        completionContext.Response.Should().BeOfType<TestState>();
        ((TestState)completionContext.Response!).Counter.Should().Be(101);
    }

    [Theory, AutoNSubstituteData]
    public async Task WithStateResponse_With_State_Mapper_Should_Map_State_To_Response(
        TestCommand command,
        StreamId streamId,
        IStateContext stateContext)
    {
        var metadata = Substitute.For<StreamMetadata>(streamId, StreamState.Active, new StreamVersion(5), DateTimeOffset.UtcNow);
        var context = new CommandContext<TestCommand>(command, metadata, stateContext);
        var projection = Substitute.For<IStateProjection<TestState>>();
        var initialState = new TestState(0);
        var evt1 = new StreamEvent(new TestEvent("event1"), EventMetadata.Empty);

        projection.CreateState(streamId).Returns(initialState);
        projection.ConsumeEvent(evt1, Arg.Any<TestState>()).Returns(new TestState(42));
        stateContext.GetState<TestState>().Returns((TestState?)null);

        context.WithStateResponse(projection, state => $"Counter: {state.Counter}");

        var completionContext = Substitute.For<ICommandCompletionContext<TestCommand>>();
        completionContext.Events.Returns([evt1]);
        completionContext.State.Returns(stateContext);

        await context.OnCompleteAsync(completionContext, CancellationToken.None);

        completionContext.Response.Should().Be("Counter: 42");
    }

    [Theory, AutoNSubstituteData]
    public async Task WithStateResponse_With_Context_Mapper_Should_Map_State_To_Response(
        TestCommand command,
        StreamId streamId,
        IStateContext stateContext)
    {
        var metadata = Substitute.For<StreamMetadata>(streamId, StreamState.Active, new StreamVersion(5), DateTimeOffset.UtcNow);
        var context = new CommandContext<TestCommand>(command, metadata, stateContext);
        var projection = Substitute.For<IStateProjection<TestState>>();
        var initialState = new TestState(0);
        var evt1 = new StreamEvent(new TestEvent("event1"), EventMetadata.Empty);

        projection.CreateState(streamId).Returns(initialState);
        projection.ConsumeEvent(evt1, Arg.Any<TestState>()).Returns(new TestState(42));
        stateContext.GetState<TestState>().Returns((TestState?)null);

        context.WithStateResponse(projection, (ctx, state) =>
            new { Command = ctx.Command.Name, Counter = state.Counter });

        var completionContext = Substitute.For<ICommandCompletionContext<TestCommand>>();
        completionContext.Command.Returns(command);
        completionContext.Events.Returns([evt1]);
        completionContext.State.Returns(stateContext);

        await context.OnCompleteAsync(completionContext, CancellationToken.None);

        completionContext.Response.Should().NotBeNull();
        var response = completionContext.Response as dynamic;
        ((string)response!.Command).Should().Be(command.Name);
        ((int)response!.Counter).Should().Be(42);
    }

    [Theory, AutoNSubstituteData]
    public async Task WithResponse_Should_Set_Response_From_Factory(
        TestCommand command,
        StreamMetadata metadata,
        IStateContext stateContext)
    {
        var context = new CommandContext<TestCommand>(command, metadata, stateContext);
        var response = new TestResponse("custom-response");

        context.WithResponse(ctx => response);

        var completionContext = Substitute.For<ICommandCompletionContext<TestCommand>>();

        await context.OnCompleteAsync(completionContext, CancellationToken.None);

        completionContext.Response.Should().Be(response);
    }

    [Theory, AutoNSubstituteData]
    public async Task WithResponse_Should_Allow_Null_Response(
        TestCommand command,
        StreamMetadata metadata,
        IStateContext stateContext)
    {
        var context = new CommandContext<TestCommand>(command, metadata, stateContext);

        context.WithResponse(ctx => null);

        var completionContext = Substitute.For<ICommandCompletionContext<TestCommand>>();

        await context.OnCompleteAsync(completionContext, CancellationToken.None);

        completionContext.Response.Should().BeNull();
    }

    [Theory, AutoNSubstituteData]
    public async Task WithResponse_Should_Have_Access_To_Completion_Context(
        TestCommand command,
        StreamMetadata metadata,
        IStateContext stateContext)
    {
        var context = new CommandContext<TestCommand>(command, metadata, stateContext);
        ICommandCompletionContext<TestCommand>? receivedContext = null;

        context.WithResponse(ctx =>
        {
            receivedContext = ctx;
            return "response";
        });

        var completionContext = Substitute.For<ICommandCompletionContext<TestCommand>>();

        await context.OnCompleteAsync(completionContext, CancellationToken.None);

        receivedContext.Should().Be(completionContext);
    }
}
