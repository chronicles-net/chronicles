# Command Handlers

Command handlers encapsulate business logic and produce events. Chronicles supports three handler patterns, each with different state and performance characteristics.

## Overview

A **command** is a request to perform an action. A **command handler** validates the command, applies business logic, and produces events.

Commands flow through the pipeline:

```
ICommandProcessor<TCommand> → ICommandHandler<TCommand> → IEventStreamWriter
```

## Three Handler Types

### 1. IStatelessCommandHandler\<TCommand\>

**Most performant** — no event replay required.

Use when your command does not need to read existing events.

```csharp
using Chronicles.Cqrs;
using Chronicles.EventStore;

public record PlaceOrder(
    string OrderId,
    string CustomerId,
    decimal TotalAmount);

public class PlaceOrderHandler : IStatelessCommandHandler<PlaceOrder>
{
    public ValueTask ExecuteAsync(
        ICommandContext<PlaceOrder> context,
        CancellationToken cancellationToken)
    {
        var cmd = context.Command;

        return context
            .AddEvent(new OrderPlaced(
                cmd.OrderId,
                cmd.CustomerId,
                cmd.TotalAmount,
                DateTimeOffset.UtcNow))
            .AsAsync();
    }
}
```

**Register:**

```csharp
builder.Services.AddChronicles(chronicles =>
{
    chronicles.WithCqrs("default", cqrs =>
    {
        cqrs.AddCommand<PlaceOrder, PlaceOrderHandler>();
    });
});
```

### 2. ICommandHandler\<TCommand, TState\>

**Replays all events** to build `TState` before executing.

Use when your command needs full aggregate state.

```csharp
using Chronicles.Cqrs;
using Chronicles.EventStore;

public record ShipOrder(string OrderId, string TrackingNumber);

public record OrderState(
    string OrderId,
    string CustomerId,
    decimal TotalAmount,
    OrderStatus Status);

public enum OrderStatus { Placed, Shipped, Cancelled }

public class ShipOrderHandler
    : ICommandHandler<ShipOrder, OrderState>
{
    public OrderState CreateState(StreamId streamId)
    {
        return new OrderState(streamId.Id, string.Empty, 0m, OrderStatus.Placed);
    }

    public OrderState? ConsumeEvent(StreamEvent evt, OrderState state)
    {
        return evt.Data switch
        {
            OrderPlaced placed => state with
            {
                CustomerId = placed.CustomerId,
                TotalAmount = placed.TotalAmount
            },
            OrderShipped => state with { Status = OrderStatus.Shipped },
            OrderCancelled => state with { Status = OrderStatus.Cancelled },
            _ => null
        };
    }

    public ValueTask ExecuteAsync(
        ICommandContext<ShipOrder> context,
        OrderState state,
        CancellationToken cancellationToken)
    {
        if (state.Status != OrderStatus.Placed)
        {
            throw new InvalidOperationException("Order cannot be shipped");
        }

        return context
            .AddEvent(new OrderShipped(
                context.Command.OrderId,
                context.Command.TrackingNumber,
                DateTimeOffset.UtcNow))
            .AsAsync();
    }
}
```

**Register:**

```csharp
cqrs.AddCommand<ShipOrder, ShipOrderHandler, OrderState>();
```

### 3. ICommandHandler\<TCommand\>

**Selective state** via `IStateContext` — only replay events you need.

Use when you need some state but not all events.

```csharp
using Chronicles.Cqrs;
using Chronicles.EventStore;

public record CancelOrder(string OrderId, string Reason);

public class CancelOrderHandler : ICommandHandler<CancelOrder>
{
    public void ConsumeEvent(
        StreamEvent evt,
        CancelOrder command,
        IStateContext state)
    {
        // Selectively track state as needed
        if (evt.Data is OrderPlaced)
        {
            state.SetState("status", OrderStatus.Placed);
        }
        else if (evt.Data is OrderShipped)
        {
            state.SetState("status", OrderStatus.Shipped);
        }
        else if (evt.Data is OrderCancelled)
        {
            state.SetState("status", OrderStatus.Cancelled);
        }
    }

    public ValueTask ExecuteAsync(
        ICommandContext<CancelOrder> context,
        CancellationToken cancellationToken)
    {
        var status = context.State.GetState<OrderStatus>("status");

        if (status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Order is already cancelled");
        }

        return context
            .AddEvent(new OrderCancelled(
                context.Command.OrderId,
                context.Command.Reason,
                DateTimeOffset.UtcNow))
            .AsAsync();
    }
}
```

**Register:**

```csharp
cqrs.AddCommand<CancelOrder, CancelOrderHandler>(new CommandOptions
{
    ConflictBehavior = CommandConflictBehavior.Retry,
    Retry = 3
});
```

## ICommandContext Fluent API

`ICommandContext<TCommand>` provides a fluent API for adding events and responses:

### AddEvent

```csharp
context.AddEvent(new OrderPlaced(...));
```

### AddEventWhen

Conditionally add an event:

```csharp
context.AddEventWhen(
    condition: ctx => ctx.Metadata.State == StreamState.New,
    addEvent: ctx => new OrderPlaced(...));
```

With state:

```csharp
context.AddEventWhen(
    given: state,
    when: (ctx, st) => st.Status == OrderStatus.Placed,
    then: (ctx, st) => new OrderShipped(...));
```

### WithResponse

Add a response returned to the caller:

```csharp
context
    .AddEvent(new OrderPlaced(...))
    .WithResponse(ctx => new { OrderId = ctx.Command.OrderId });
```

### WithStateResponse

Return the updated state as the response:

```csharp
context
    .AddEvent(new OrderShipped(...))
    .WithStateResponse(this); // 'this' is IStateProjection<OrderState>
```

### AsAsync

Convert the context to a `ValueTask`:

```csharp
return context
    .AddEvent(new OrderPlaced(...))
    .AsAsync();
```

## CommandOptions

Configure command behavior:

```csharp
var options = new CommandOptions
{
    RequiredState = StreamState.New,               // Require stream to be new
    ConflictBehavior = CommandConflictBehavior.Retry, // Retry on conflict
    Retry = 5,                                     // Retry up to 5 times
    Consistency = CommandConsistency.ReadWrite     // Consistency level
};

cqrs.AddCommand<PlaceOrder, PlaceOrderHandler>(options);
```

**CommandConflictBehavior:**

- `Fail`: Throw `StreamConflictException` on conflict (default)
- `Retry`: Retry command execution up to `Retry` times

**CommandConsistency:**

- `ReadWrite`: Full read/write consistency (default)
- `Write`: Write-only consistency

## CommandResult

Command execution returns a `CommandResult`:

```csharp
var result = await processor.ExecuteAsync(
    new StreamId("order", "ord-123"),
    new PlaceOrder("ord-123", "cust-456", 99.99m),
    cancellationToken: cancellationToken);

Console.WriteLine($"StreamId: {result.Id}");
Console.WriteLine($"Version: {result.Version}");
Console.WriteLine($"Result: {result.Result}");      // Changed or NotModified
Console.WriteLine($"Response: {result.Response}");  // Custom response object
```

**ResultType values:**

- `Changed`: Events were appended
- `NotModified`: No events were appended
- `Conflict`: Stream version conflict occurred

## ICommandProcessor\<TCommand\>

Execute commands using `ICommandProcessor<TCommand>`:

```csharp
public class OrderController
{
    private readonly ICommandProcessor<PlaceOrder> _processor;

    public OrderController(ICommandProcessor<PlaceOrder> processor)
    {
        _processor = processor;
    }

    public async Task<IActionResult> PlaceOrderAsync(
        string orderId,
        string customerId,
        decimal totalAmount,
        CancellationToken cancellationToken)
    {
        var streamId = new StreamId("order", orderId);
        var command = new PlaceOrder(orderId, customerId, totalAmount);

        var result = await _processor.ExecuteAsync(
            streamId,
            command,
            cancellationToken: cancellationToken);

        return Ok(new { OrderId = orderId, Version = result.Version });
    }
}
```

## ICommandProcessorFactory

For dynamic command processor resolution, use `ICommandProcessorFactory`:

```csharp
public class CommandRouter
{
    private readonly ICommandProcessorFactory _factory;

    public CommandRouter(ICommandProcessorFactory factory)
    {
        _factory = factory;
    }

    public async Task ExecuteAsync<TCommand>(
        StreamId streamId,
        TCommand command,
        CancellationToken cancellationToken)
        where TCommand : class
    {
        var processor = _factory.Create<TCommand>();
        await processor.ExecuteAsync(streamId, command, cancellationToken: cancellationToken);
    }
}
```

## Best Practices

- **Use `IStatelessCommandHandler<T>` when possible** for best performance
- **Use `ICommandHandler<T, TState>` when full state is needed** — simplest to reason about
- **Use `ICommandHandler<T>` when selective state is needed** — balance of performance and state
- **Keep commands focused** on a single operation
- **Use `AddEventWhen` to make command logic declarative**
- **Return responses with `WithResponse` or `WithStateResponse`** for query-after-write patterns
- **Configure retries** for idempotent commands to handle transient conflicts
