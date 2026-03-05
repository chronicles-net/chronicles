# Chronicles

Event sourcing and CQRS for .NET, backed by Azure Cosmos DB. Chronicles gives you append-only event streams, command handlers, state projections, and document projections — production-ready infrastructure you'd otherwise spend months building yourself.

[![CI](https://github.com/chronicles-net/chronicles/actions/workflows/ci.yml/badge.svg)](https://github.com/chronicles-net/chronicles/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Chronicles.svg)](https://www.nuget.org/packages/Chronicles)
[![Branch Coverage](.github/coveragereport/badge_branchcoverage.svg?raw=true)](https://github.com/chronicles-net/chronicles/actions/workflows/ci.yml)
[![Line Coverage](.github/coveragereport/badge_linecoverage.svg?raw=true)](https://github.com/chronicles-net/chronicles/actions/workflows/ci.yml)
[![Method Coverage](.github/coveragereport/badge_methodcoverage.svg?raw=true)](https://github.com/chronicles-net/chronicles/actions/workflows/ci.yml)

## Install

```sh
dotnet add package Chronicles
```

## Quick start

```csharp
// Define your events
record OrderPlaced(string OrderId, string CustomerId, decimal Total);
record OrderShipped(string OrderId, DateTimeOffset ShippedAt);

// Write events to a stream
var streamId = new StreamId("order", orderId);

await writer.WriteAsync(streamId, [
    new OrderPlaced(orderId, customerId, 49.99m),
]);

// Later: append more events
await writer.WriteAsync(streamId, [
    new OrderShipped(orderId, DateTimeOffset.UtcNow),
]);

// Read events back
await foreach (var evt in reader.ReadAsync(streamId))
{
    Console.WriteLine($"{evt.EventType} at {evt.Metadata.Timestamp}");
}
```

## Three pillars

### Event Store

Append-only, versioned event streams persisted to Cosmos DB with optimistic concurrency.

| Type | Role |
|---|---|
| `StreamId` | Identifies a stream by category + id (e.g. `"order.42"`) |
| `IEventStreamWriter` | Appends events, closes streams, manages checkpoints |
| `IEventStreamReader` | Reads events, queries streams, retrieves checkpoints |

### CQRS

Command handlers validate business rules, emit events, and rebuild state from event history.

| Type | Role |
|---|---|
| `ICommandHandler<TCommand, TState>` | Handles a command with full state projection |
| `IStatelessCommandHandler<TCommand>` | Handles a command without replaying all events |
| `IStateProjection<TState>` | Folds a stream of events into aggregate state |

```csharp
// Implement a stateful command handler
public class ShipOrderHandler : ICommandHandler<ShipOrder, OrderState>
{
    public OrderState CreateState(StreamId streamId) => new();

    public OrderState? ConsumeEvent(StreamEvent evt, OrderState state) =>
        evt.Data switch
        {
            OrderPlaced p => state with { CustomerId = p.CustomerId, Total = p.Total },
            _ => null,
        };

    public async ValueTask ExecuteAsync(
        ICommandContext<ShipOrder> context,
        OrderState state,
        CancellationToken cancellationToken)
    {
        if (state.CustomerId is null)
            throw new InvalidOperationException("Order does not exist.");

        context.AddEvent(new OrderShipped(context.Command.OrderId, DateTimeOffset.UtcNow));
    }
}
```

### Document Store

Read-model projections driven by the Cosmos DB change feed.

| Type | Role |
|---|---|
| `IDocumentProjection<TDocument>` | Projects events into a Cosmos read-model document |
| `IDocumentWriter<T>` | Creates, updates, replaces, and deletes documents |
| `IDocumentReader<T>` | Queries documents from Cosmos |

## Sample

The [`sample/`](sample/) directory contains a food-delivery microservices demo built with .NET Aspire. It covers three bounded contexts — **Orders**, **Restaurants**, and **Couriers** — and shows how to wire up event streams, command handlers, and document projections end-to-end.

## Contributing & community

- **Questions**: [GitHub Discussions](https://github.com/chronicles-net/chronicles/discussions)
- **Bugs / features**: [GitHub Issues](https://github.com/chronicles-net/chronicles/issues)
- **Contributing**: see [CONTRIBUTING.md](CONTRIBUTING.md)
- **Code of conduct**: see [CODE-OF-CONDUCT.md](CODE-OF-CONDUCT.md)