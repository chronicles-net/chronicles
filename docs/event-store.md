# Event Store

The Chronicles event store provides append-only event streams backed by Azure Cosmos DB. Events are the source of truth in an event-sourced system.

## Core Concepts

### StreamId

A `StreamId` uniquely identifies an event stream and consists of two parts:

- **Category**: The type of stream (e.g., `"order"`, `"customer"`)
- **Id**: The unique identifier within that category (e.g., order ID, customer ID)

```csharp
using Chronicles.EventStore;

var streamId = new StreamId("order", "ord-12345");

// Access parts
Console.WriteLine(streamId.Category); // "order"
Console.WriteLine(streamId.Id);       // "ord-12345"

// Convert to string
string streamIdString = (string)streamId; // "order.ord-12345"

// Parse from string
var parsed = StreamId.FromString("order.ord-12345");
```

### Events

Events are immutable records representing facts that have occurred. Register events with versioned names:

```csharp
builder.Services.AddChronicles(store =>
{
    store.WithEventStore(eventStore =>
    {
        eventStore.AddEvent<OrderPlaced>("order-placed:v1");
        eventStore.AddEvent<OrderShipped>("order-shipped:v1");
        eventStore.AddEvent<OrderCancelled>("order-cancelled:v1");
    });
});
```

**Event records:**

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

### StreamVersion

Each event appended to a stream increments the stream's version. Versions start at `1` and are sequential.

```csharp
StreamVersion version = StreamVersion.New;        // 0 (stream does not exist)
StreamVersion v1 = StreamVersion.FromLong(1);     // First event
StreamVersion v5 = StreamVersion.FromLong(5);     // Fifth event

bool isNew = version == StreamVersion.New;        // true
```

## Writing Events

### IEventStreamWriter

Use `IEventStreamWriter` to append events to a stream:

```csharp
using System.Collections.Immutable;
using Chronicles.EventStore;

public class OrderService
{
    private readonly IEventStreamWriter _writer;

    public OrderService(IEventStreamWriter writer)
    {
        _writer = writer;
    }

    public async Task PlaceOrderAsync(
        string orderId,
        string customerId,
        decimal totalAmount,
        CancellationToken cancellationToken = default)
    {
        var streamId = new StreamId("order", orderId);

        var events = ImmutableList<object>.Empty
            .Add(new OrderPlaced(orderId, customerId, totalAmount, DateTimeOffset.UtcNow));

        var result = await _writer.WriteAsync(streamId, events, cancellationToken: cancellationToken);

        Console.WriteLine($"Stream version after write: {result.Version}");
    }
}
```

### StreamWriteResult

`WriteAsync` returns a `StreamWriteResult` containing:

- `Version`: The stream version after the write
- `Result`: `ResultType.Changed` or `ResultType.NotModified`

### Optimistic Concurrency

Use `StreamWriteOptions` to enforce optimistic concurrency with expected versions:

```csharp
var options = new StreamWriteOptions
{
    ExpectedVersion = StreamVersion.FromLong(3) // Require stream to be at version 3
};

try
{
    await _writer.WriteAsync(streamId, events, options, cancellationToken: cancellationToken);
}
catch (StreamConflictException ex)
{
    // Handle conflict: stream version does not match expected
    Console.WriteLine($"Conflict: expected {ex.ExpectedVersion}, actual {ex.ActualVersion}");
}
```

### Close and Delete

Close a stream to prevent further writes:

```csharp
await _writer.CloseAsync(streamId, cancellationToken: cancellationToken);
```

Delete a stream and all its events:

```csharp
await _writer.DeleteStreamAsync(streamId, cancellationToken: cancellationToken);
```

## Reading Events

### IEventStreamReader

Use `IEventStreamReader` to read events from a stream:

```csharp
using Chronicles.EventStore;

public class OrderQueryService
{
    private readonly IEventStreamReader _reader;

    public OrderQueryService(IEventStreamReader reader)
    {
        _reader = reader;
    }

    public async Task<List<object>> GetOrderEventsAsync(
        string orderId,
        CancellationToken cancellationToken = default)
    {
        var streamId = new StreamId("order", orderId);
        var events = new List<object>();

        await foreach (var evt in _reader.ReadAsync(streamId, cancellationToken: cancellationToken))
        {
            events.Add(evt.Data);
        }

        return events;
    }
}
```

### StreamEvent

Each event read from the stream is wrapped in a `StreamEvent`:

```csharp
await foreach (var evt in _reader.ReadAsync(streamId, cancellationToken: cancellationToken))
{
    Console.WriteLine($"Version: {evt.Version}");
    Console.WriteLine($"Event Type: {evt.EventType}");
    Console.WriteLine($"Timestamp: {evt.Timestamp}");
    Console.WriteLine($"Data: {evt.Data}");
}
```

### StreamReadOptions

Filter events by version range:

```csharp
var options = new StreamReadOptions
{
    FromVersion = StreamVersion.FromLong(5),  // Start at version 5
    ToVersion = StreamVersion.FromLong(10)    // End at version 10
};

await foreach (var evt in _reader.ReadAsync(streamId, options, cancellationToken: cancellationToken))
{
    // Process events from version 5 to 10
}
```

### Stream Metadata

Get stream state and version without reading all events:

```csharp
var metadata = await _reader.GetMetadataAsync(streamId, cancellationToken: cancellationToken);

Console.WriteLine($"State: {metadata.State}");       // New, Active, Closed
Console.WriteLine($"Version: {metadata.Version}");
Console.WriteLine($"StreamId: {metadata.StreamId}");
Console.WriteLine($"Created: {metadata.CreatedAt}");
```

**StreamState values:**

- `StreamState.New`: Stream does not exist
- `StreamState.Active`: Stream exists and can accept writes
- `StreamState.Closed`: Stream is closed and cannot accept writes

### Query Streams

Search for streams matching a filter:

```csharp
await foreach (var metadata in _reader.QueryStreamsAsync(
    filter: "order.*",
    cancellationToken: cancellationToken))
{
    Console.WriteLine($"Stream: {metadata.StreamId}, Version: {metadata.Version}");
}
```

## Checkpoints

Set named checkpoints within a stream to mark specific versions:

```csharp
// Set a checkpoint
await _writer.SetCheckpointAsync(
    name: "shipped",
    streamId: streamId,
    version: StreamVersion.FromLong(5),
    state: null,
    cancellationToken: cancellationToken);

// Get a checkpoint
var checkpoint = await _reader.GetCheckpointAsync<object>(
    name: "shipped",
    streamId: streamId,
    cancellationToken: cancellationToken);

if (checkpoint != null)
{
    Console.WriteLine($"Checkpoint version: {checkpoint.Version}");
}
```

## Best Practices

- **Use immutable records** for events
- **Version your event names** (e.g., `"order-placed:v1"`) to support schema evolution
- **Keep streams focused** on a single aggregate
- **Use optimistic concurrency** when version matters
- **Avoid large streams** — consider stream archival strategies for unbounded growth
