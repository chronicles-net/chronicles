# Getting Started with Chronicles

Chronicles is a .NET event sourcing and CQRS library backed by Azure Cosmos DB. This guide walks you through installation and your first read/write operations.

## Installation

Install the Chronicles NuGet package:

```bash
dotnet add package Chronicles
```

## Configuration

Register Chronicles in your dependency injection container using `AddChronicles`:

```csharp
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddChronicles(store =>
{
    store.Configure(options =>
    {
        options.UseConnectionString("your-cosmos-connection-string");
        options.UseDatabase("your-database-name");
    });

    store.WithEventStore(eventStore =>
    {
        eventStore.AddEvent<OrderPlaced>("order-placed:v1");
        eventStore.AddEvent<OrderShipped>("order-shipped:v1");
        eventStore.AddEvent<OrderCancelled>("order-cancelled:v1");
    });
});

var app = builder.Build();
app.Run();
```

## Define Your Events

Events are immutable records representing facts that have occurred:

```csharp
public record OrderPlaced(
    string OrderId,
    string CustomerId,
    decimal TotalAmount,
    DateTimeOffset PlacedAt);

public record OrderShipped(
    string OrderId,
    string TrackingNumber,
    DateTimeOffset ShippedAt);

public record OrderCancelled(
    string OrderId,
    string Reason,
    DateTimeOffset CancelledAt);
```

## Writing Events

Use `IEventStreamWriter` to append events to a stream:

```csharp
using Chronicles.EventStore;
using System.Collections.Immutable;

public class OrderService
{
    private readonly IEventStreamWriter writer;

    public OrderService(IEventStreamWriter writer)
    {
        this.writer = writer;
    }

    public async Task PlaceOrderAsync(
        string orderId,
        string customerId,
        decimal totalAmount,
        CancellationToken cancellationToken = default)
    {
        var streamId = new StreamId("order", orderId);

        var events = ImmutableList<object>.Empty.Add(
            new OrderPlaced(orderId, customerId, totalAmount, DateTimeOffset.UtcNow)
        );

        await writer.WriteAsync(streamId, events, cancellationToken: cancellationToken);
     }
}
```

### Write Options

You can provide additional context when writing events using `StreamWriteOptions`:

```csharp
using Chronicles.EventStore;

var options = new StreamWriteOptions
{
    CorrelationId = "correlation-123",  // Links events across streams
    CausationId = "command-456"         // Links events in a causality chain
};

await _writer.WriteAsync(
    streamId,
    events,
    options: options,
    cancellationToken: cancellationToken);
```

The correlation and causation IDs are stored in each event's metadata and help trace operations across distributed systems.

## Reading Events

Use `IEventStreamReader` to read events from a stream:

```csharp
using Chronicles.EventStore;

public class OrderQueryService
{
    private readonly IEventStreamReader reader;

    public OrderQueryService(IEventStreamReader reader)
    {
        this.reader = reader;
    }

    public async Task<OrderState?> GetOrderAsync(
        string orderId,
        CancellationToken cancellationToken = default)
    {
        var streamId = new StreamId("order", orderId);

        var metadata = await reader.GetMetadataAsync(streamId, cancellationToken: cancellationToken);
        if (metadata.State == StreamState.New)
        {
            return null;
        }

        OrderState? state = null;

        await foreach (var evt in reader.ReadAsync(streamId, cancellationToken: cancellationToken))
        {
            state = evt.Data switch
            {
                OrderPlaced placed => new OrderState(
                    placed.OrderId,
                    placed.CustomerId,
                    placed.TotalAmount,
                    OrderStatus.Placed,
                    placed.PlacedAt,
                    null,
                    null),
                OrderShipped shipped when state != null => state with
                {
                    Status = OrderStatus.Shipped,
                    TrackingNumber = shipped.TrackingNumber,
                    ShippedAt = shipped.ShippedAt
                },
                OrderCancelled cancelled when state != null => state with
                {
                    Status = OrderStatus.Cancelled
                },
                _ => state
            };
        }

        return state;
    }
}

public record OrderState(
    string OrderId,
    string CustomerId,
    decimal TotalAmount,
    OrderStatus Status,
    DateTimeOffset PlacedAt,
    string? TrackingNumber,
    DateTimeOffset? ShippedAt);

public enum OrderStatus
{
    Placed,
    Shipped,
    Cancelled
}
```

## Next Steps

- **[Event Store](event-store.md)**: Learn about streams, versions, and optimistic concurrency
- **[Command Handlers](command-handlers.md)**: Use CQRS commands to encapsulate business logic
- **[Projections](projections.md)**: Build read models from event streams
- **[Document Store](document-store.md)**: Query and write documents to Cosmos DB
- **[Testing](testing.md)**: Test your event sourcing logic with `AddFakeChronicles`
