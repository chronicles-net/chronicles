# Projections

Projections build read models from event streams. Chronicles provides two projection types: **state projections** for in-memory state and **document projections** for persisted read models.

## IStateProjection\<TState\>

State projections fold events into an in-memory state object.

```csharp
using Chronicles.Cqrs;
using Chronicles.EventStore;

public record OrderState(
    string OrderId,
    string CustomerId,
    decimal TotalAmount,
    OrderStatus Status,
    DateTimeOffset PlacedAt,
    string? TrackingNumber,
    DateTimeOffset? ShippedAt);

public enum OrderStatus { Placed, Shipped, Cancelled }

public class OrderStateProjection : IStateProjection<OrderState>
{
    public OrderState CreateState(StreamId streamId)
    {
        return new OrderState(
            streamId.Id,
            string.Empty,
            0m,
            OrderStatus.Placed,
            DateTimeOffset.MinValue,
            null,
            null);
    }

    public OrderState? ConsumeEvent(StreamEvent evt, OrderState state)
    {
        return evt.Data switch
        {
            OrderPlaced placed => state with
            {
                CustomerId = placed.CustomerId,
                TotalAmount = placed.TotalAmount,
                PlacedAt = placed.PlacedAt
            },
            OrderShipped shipped => state with
            {
                Status = OrderStatus.Shipped,
                TrackingNumber = shipped.TrackingNumber,
                ShippedAt = shipped.ShippedAt
            },
            OrderCancelled => state with
            {
                Status = OrderStatus.Cancelled
            },
            _ => null
        };
    }
}
```

**Usage:**

State projections are used by `ICommandHandler<TCommand, TState>` to rebuild aggregate state before command execution.

## IDocumentProjection\<TDocument\>

Document projections extend `IStateProjection<TDocument>` and persist the resulting document to Cosmos DB.

### Defining a Document

Documents must implement `IDocument`:

```csharp
using Chronicles.Documents;
using System.Text.Json.Serialization;

[ContainerName("orders")]
public record OrderDocument(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("pk")] string PartitionKey,
    string CustomerId,
    decimal TotalAmount,
    OrderStatus Status,
    DateTimeOffset PlacedAt,
    string? TrackingNumber,
    DateTimeOffset? ShippedAt) : IDocument
{
    public string GetDocumentId() => Id;
    public string GetPartitionKey() => PartitionKey;
}
```

**Key requirements:**

- Implement `IDocument` with `GetDocumentId()` and `GetPartitionKey()`
- Use `[JsonPropertyName("id")]` for document ID
- Use `[JsonPropertyName("pk")]` for partition key
- Use `[ContainerName("orders")]` to specify Cosmos container

### Creating a Document Projection

```csharp
using Chronicles.Cqrs;
using Chronicles.Documents;
using Chronicles.EventStore;

public class OrderDocumentProjection : IDocumentProjection<OrderDocument>
{
    public OrderDocument CreateState(StreamId streamId)
    {
        return new OrderDocument(
            Id: streamId.Id,
            PartitionKey: streamId.Id,
            CustomerId: string.Empty,
            TotalAmount: 0m,
            Status: OrderStatus.Placed,
            PlacedAt: DateTimeOffset.MinValue,
            TrackingNumber: null,
            ShippedAt: null);
    }

    public OrderDocument? ConsumeEvent(StreamEvent evt, OrderDocument document)
    {
        return evt.Data switch
        {
            OrderPlaced placed => document with
            {
                CustomerId = placed.CustomerId,
                TotalAmount = placed.TotalAmount,
                PlacedAt = placed.PlacedAt
            },
            OrderShipped shipped => document with
            {
                Status = OrderStatus.Shipped,
                TrackingNumber = shipped.TrackingNumber,
                ShippedAt = shipped.ShippedAt
            },
            OrderCancelled => document with
            {
                Status = OrderStatus.Cancelled
            },
            _ => null
        };
    }

    public ValueTask<DocumentCommitAction> OnCommitAsync(
        OrderDocument document,
        CancellationToken cancellationToken)
    {
        // Control what happens when the projection is committed
        if (document.Status == OrderStatus.Cancelled)
        {
            return ValueTask.FromResult(DocumentCommitAction.Delete);
        }

        return ValueTask.FromResult(DocumentCommitAction.Update);
    }
}
```

### DocumentCommitAction

`OnCommitAsync` returns a `DocumentCommitAction` to control persistence:

- `Update`: Upsert the document (default)
- `Delete`: Delete the document
- `None`: Do not persist any changes

### Registering Document Projections

Subscribe to the event stream change feed to automatically update documents:

```csharp
builder.Services.AddChronicles(store =>
{
    store.Configure(options =>
    {
        options.UseConnectionString("your-connection-string");
        options.UseDatabase("your-database");
    });

    store.WithEventStore(eventStore =>
    {
        eventStore.AddEvent<OrderPlaced>("order-placed:v1");
        eventStore.AddEvent<OrderShipped>("order-shipped:v1");
        eventStore.AddEvent<OrderCancelled>("order-cancelled:v1");

        eventStore.AddEventSubscription("order-projection", subscription =>
        {
            subscription.MapAllStreams(processor =>
            {
                processor.AddDocumentProjection<OrderDocument, OrderDocumentProjection>();
            });
        });
    });
});
```

## IDocumentProjectionRebuilder

Use `IDocumentProjectionRebuilder` to rebuild projections from an event stream:

```csharp
using Chronicles.Cqrs;

public class OrderProjectionRebuilder
{
    private readonly IDocumentProjectionRebuilder<OrderDocumentProjection, OrderDocument> _rebuilder;

    public OrderProjectionRebuilder(
        IDocumentProjectionRebuilder<OrderDocumentProjection, OrderDocument> rebuilder)
    {
        _rebuilder = rebuilder;
    }

    public async Task<OrderDocument> RebuildOrderProjectionAsync(
        string orderId,
        CancellationToken cancellationToken = default)
    {
        var streamId = new StreamId("order", orderId);

        return await _rebuilder.RebuildAsync(streamId, cancellationToken);
    }
}
```

## IDocumentPublisher

Use `IDocumentPublisher<TDocument>` to publish documents to external systems after projection processing:

```csharp
using Chronicles.Cqrs;

public class OrderDocumentPublisher : IDocumentPublisher<OrderDocument>
{
    public async Task PublishAsync(
        OrderDocument document,
        CancellationToken cancellationToken)
    {
        // Publish to external system (e.g., message bus, search index)
    }
}
```

Register with `AddPublishingDocumentProjection` in the event subscription:

```csharp
eventStore.AddEventSubscription("order-projection", subscription =>
{
    subscription.MapAllStreams(processor =>
    {
        processor.AddPublishingDocumentProjection<OrderDocument, OrderDocumentProjection, OrderDocumentPublisher>();
    });
});
```

## Event Subscriptions with Multiple Projections

Register multiple projections in a single subscription:

```csharp
eventStore.AddEventSubscription("order-processing", subscription =>
{
    subscription.MapAllStreams(processor =>
    {
        processor.AddDocumentProjection<OrderDocument, OrderDocumentProjection>();
        processor.AddDocumentProjection<OrderSummaryDocument, OrderSummaryProjection>();
        processor.AddStateProjection<OrderState, OrderStateProjection>();
    });
});
```

## Container Configuration

Documents are stored in Cosmos containers specified by the `[ContainerName]` attribute:

```csharp
[ContainerName("orders")]
public record OrderDocument(...) : IDocument { }

[ContainerName("order-summaries")]
public record OrderSummaryDocument(...) : IDocument { }
```

Chronicles will create containers automatically if they do not exist (requires appropriate Cosmos DB permissions).

## Projection Strategies

### Strategy 1: One Document per Stream

Project each stream to a single document:

```csharp
public OrderDocument CreateState(StreamId streamId)
{
    return new OrderDocument(
        Id: streamId.Id,           // Document ID = Stream ID
        PartitionKey: streamId.Id, // Partition key = Stream ID
        ...);
}
```

### Strategy 2: Multiple Documents per Stream

Project a stream to multiple documents (e.g., one per line item):

```csharp
public OrderLineDocument? ConsumeEvent(StreamEvent evt, OrderLineDocument document)
{
    if (evt.Data is OrderLineAdded added)
    {
        return new OrderLineDocument(
            Id: $"{added.OrderId}-{added.LineId}",
            PartitionKey: added.OrderId,
            LineId: added.LineId,
            ProductId: added.ProductId,
            Quantity: added.Quantity);
    }

    return null;
}
```

### Strategy 3: Cross-Stream Aggregation

Project multiple streams into a single aggregate document:

```csharp
public CustomerSummaryDocument? ConsumeEvent(StreamEvent evt, CustomerSummaryDocument document)
{
    return evt.Data switch
    {
        OrderPlaced placed => document with
        {
            TotalOrders = document.TotalOrders + 1,
            TotalSpent = document.TotalSpent + placed.TotalAmount
        },
        _ => null
    };
}
```

## Best Practices

- **Return `null` from `ConsumeEvent` if the event does not affect the document** — this avoids unnecessary writes
- **Use `OnCommitAsync` to control document lifecycle** (update vs delete)
- **Use `[ContainerName]` to organize documents** by bounded context
- **Keep projections idempotent** — same events always produce the same document
- **Use `IDocumentProjectionRebuilder` to rebuild projections** when schema changes
- **Partition documents thoughtfully** — align with query patterns
