# EventStore Layer

The EventStore layer is the heart of Chronicles' event sourcing capabilities. It provides robust, scalable event persistence and streaming on top of Azure Cosmos DB, enabling you to store and retrieve events with strong consistency guarantees.

## 🎯 Core Concepts

### Event Streams

Events are organized into **streams**, which are ordered sequences of events related to a specific aggregate or entity. Each stream is identified by a `StreamId`:

```csharp
// Simple stream ID
var orderStreamId = new StreamId("order", "12345");

// Composite stream ID for hierarchical organization
var orderItemStreamId = new StreamId("order-item", "12345", "item-1");

// Stream ID from string
var streamId = StreamId.FromString("customer.98765");
```

### Events and Metadata

Every event in Chronicles consists of:
- **Data**: The actual event payload (your domain event)
- **Metadata**: System-generated information about the event

```csharp
public record OrderCreated(string OrderId, string CustomerId, decimal Amount);

// When stored, becomes a StreamEvent
var streamEvent = new StreamEvent(
    Data: new OrderCreated("order-123", "customer-456", 99.99m),
    Metadata: new EventMetadata(
        Name: "OrderCreated",
        CorrelationId: "correlation-123", 
        CausationId: "command-456",
        StreamId: new StreamId("order", "order-123"),
        Timestamp: DateTimeOffset.UtcNow,
        Version: new StreamVersion(1)
    )
);
```

### Stream Versions

Streams maintain strict ordering through versions:

```csharp
// Version control constants
StreamVersion.RequireEmpty     // Stream must be empty (new stream)
StreamVersion.Any              // Any version (append to existing)
StreamVersion.RequireNotEmpty  // Stream must exist

// Specific version (optimistic concurrency)
var expectedVersion = new StreamVersion(5);
```

## 📝 Writing Events

Use `IEventStreamWriter` to persist events to streams:

### Basic Event Writing

```csharp
public class OrderService
{
    private readonly IEventStreamWriter _eventWriter;

    public OrderService(IEventStreamWriter eventWriter)
    {
        _eventWriter = eventWriter;
    }

    public async Task CreateOrderAsync(string orderId, string customerId, decimal amount)
    {
        var streamId = new StreamId("order", orderId);
        var events = ImmutableList.Create<object>(
            new OrderCreated(orderId, customerId, amount)
        );

        var result = await _eventWriter.WriteAsync(
            streamId: streamId,
            events: events,
            options: new StreamWriteOptions
            {
                ExpectedVersion = StreamVersion.RequireEmpty, // Ensure new stream
                CorrelationId = "web-request-123",
                CausationId = "create-order-command"
            }
        );

        Console.WriteLine($"Events written to version {result.Version}");
    }
}
```

### Writing Multiple Events

```csharp
public async Task ProcessOrderWorkflowAsync(string orderId)
{
    var streamId = new StreamId("order", orderId);
    var events = ImmutableList.Create<object>(
        new OrderCreated(orderId, "customer-123", 99.99m),
        new OrderItemAdded(orderId, "product-1", 2, 49.99m),
        new OrderValidated(orderId),
        new OrderSubmitted(orderId, DateTime.UtcNow)
    );

    var result = await _eventWriter.WriteAsync(streamId, events);
    
    // result.Version will be 4 (if starting from empty stream)
    // result.StreamState will be StreamState.Active
}
```

### Conditional Writes (Optimistic Concurrency)

```csharp
public async Task UpdateOrderAsync(string orderId, decimal newAmount)
{
    var streamId = new StreamId("order", orderId);
    
    // Get current version first
    var metadata = await _eventReader.GetMetadataAsync(streamId);
    
    try
    {
        var events = ImmutableList.Create<object>(
            new OrderAmountChanged(orderId, newAmount)
        );
        
        await _eventWriter.WriteAsync(
            streamId: streamId,
            events: events,
            options: new StreamWriteOptions
            {
                ExpectedVersion = metadata.Version // Ensure no concurrent changes
            }
        );
    }
    catch (StreamConflictException)
    {
        // Handle concurrent modification
        throw new ConcurrencyException("Order was modified by another process");
    }
}
```

### Stream Management

```csharp
public async Task CloseOrderStreamAsync(string orderId)
{
    var streamId = new StreamId("order", orderId);
    
    // Prevent further writes to this stream
    await _eventWriter.CloseAsync(streamId);
}

public async Task DeleteOrderStreamAsync(string orderId)
{
    var streamId = new StreamId("order", orderId);
    
    // Permanently delete stream and all events (use with caution!)
    await _eventWriter.DeleteStreamAsync(streamId);
}
```

## 📖 Reading Events

Use `IEventStreamReader` to retrieve events from streams:

### Reading All Events

```csharp
public class OrderProjectionService
{
    private readonly IEventStreamReader _eventReader;

    public OrderProjectionService(IEventStreamReader eventReader)
    {
        _eventReader = eventReader;
    }

    public async Task<OrderState> GetOrderStateAsync(string orderId)
    {
        var streamId = new StreamId("order", orderId);
        var state = new OrderState();

        await foreach (var evt in _eventReader.ReadAsync(streamId))
        {
            state = ApplyEvent(state, evt);
        }

        return state;
    }

    private OrderState ApplyEvent(OrderState state, StreamEvent evt)
    {
        return evt.Data switch
        {
            OrderCreated e => state with 
            { 
                Id = e.OrderId, 
                CustomerId = e.CustomerId, 
                Amount = e.Amount,
                Status = OrderStatus.Created
            },
            OrderItemAdded e => state with 
            { 
                Items = state.Items.Add(new OrderItem(e.ProductId, e.Quantity, e.UnitPrice))
            },
            OrderSubmitted e => state with { Status = OrderStatus.Submitted },
            _ => state
        };
    }
}
```

### Reading with Options

```csharp
public async Task<List<StreamEvent>> GetRecentOrderEventsAsync(string orderId)
{
    var streamId = new StreamId("order", orderId);
    var events = new List<StreamEvent>();

    // Read events from version 10 onwards
    await foreach (var evt in _eventReader.ReadAsync(
        streamId,
        options: new StreamReadOptions
        {
            FromVersion = new StreamVersion(10),
            ToVersion = StreamVersion.Any,
            MaxCount = 50 // Limit results
        }))
    {
        events.Add(evt);
    }

    return events;
}
```

### Stream Metadata

```csharp
public async Task<StreamInfo> GetStreamInfoAsync(string orderId)
{
    var streamId = new StreamId("order", orderId);
    var metadata = await _eventReader.GetMetadataAsync(streamId);

    return new StreamInfo
    {
        StreamId = metadata.StreamId,
        State = metadata.State,
        CurrentVersion = metadata.Version,
        LastUpdated = metadata.Timestamp
    };
}

public record StreamInfo
{
    public StreamId StreamId { get; init; }
    public StreamState State { get; init; }
    public StreamVersion CurrentVersion { get; init; }
    public DateTimeOffset LastUpdated { get; init; }
}
```

### Querying Streams

```csharp
public async Task<List<StreamMetadata>> FindOrderStreamsAsync(string customerId)
{
    var streams = new List<StreamMetadata>();
    
    // Find streams by pattern
    await foreach (var stream in _eventReader.QueryStreamsAsync(
        filter: $"STARTSWITH(c.streamId, 'order.{customerId}-')",
        createdAfter: DateTime.UtcNow.AddDays(-30)
    ))
    {
        streams.Add(stream);
    }
    
    return streams;
}
```

## 🔄 Checkpoints

Checkpoints allow you to mark processing positions within streams and store associated state:

### Setting Checkpoints

```csharp
public async Task ProcessOrderEventsAsync(string orderId)
{
    var streamId = new StreamId("order", orderId);
    var processingState = new OrderProcessingState();

    await foreach (var evt in _eventReader.ReadAsync(streamId))
    {
        // Process event
        processingState = await ProcessEvent(processingState, evt);
        
        // Save checkpoint with state every 10 events
        if (evt.Metadata.Version.Value % 10 == 0)
        {
            await _eventWriter.SetCheckpointAsync(
                name: "order-processor",
                streamId: streamId,
                version: evt.Metadata.Version,
                state: processingState
            );
        }
    }
}
```

### Reading Checkpoints

```csharp
public async Task ResumeOrderProcessingAsync(string orderId)
{
    var streamId = new StreamId("order", orderId);
    
    // Get last checkpoint
    var checkpoint = await _eventReader.GetCheckpointAsync<OrderProcessingState>(
        name: "order-processor",
        streamId: streamId
    );

    StreamVersion startVersion;
    OrderProcessingState state;
    
    if (checkpoint != null)
    {
        // Resume from checkpoint
        startVersion = checkpoint.StreamVersion + 1;
        state = checkpoint.State;
    }
    else
    {
        // Start from beginning
        startVersion = StreamVersion.Any;
        state = new OrderProcessingState();
    }

    await foreach (var evt in _eventReader.ReadAsync(
        streamId,
        options: new StreamReadOptions { FromVersion = startVersion }
    ))
    {
        state = await ProcessEvent(state, evt);
    }
}
```

## 🚀 Event Processing

Chronicles provides patterns for processing events as they arrive:

### Simple Event Processor

```csharp
public class OrderEventProcessor : IEventProcessor
{
    private readonly ILogger<OrderEventProcessor> _logger;

    public OrderEventProcessor(ILogger<OrderEventProcessor> logger)
    {
        _logger = logger;
    }

    public async ValueTask ConsumeAsync(
        StreamEvent evt, 
        IStateContext state, 
        bool hasMore, 
        CancellationToken cancellationToken)
    {
        switch (evt.Data)
        {
            case OrderCreated orderCreated:
                await HandleOrderCreated(orderCreated, state);
                break;
                
            case OrderSubmitted orderSubmitted:
                await HandleOrderSubmitted(orderSubmitted, state);
                break;
                
            default:
                _logger.LogDebug("Unhandled event type: {EventType}", evt.Data.GetType().Name);
                break;
        }
    }

    private async Task HandleOrderCreated(OrderCreated evt, IStateContext state)
    {
        // Update projection, send notifications, etc.
        var currentState = state.Get<OrderProjection>() ?? new OrderProjection();
        currentState.Id = evt.OrderId;
        currentState.CustomerId = evt.CustomerId;
        currentState.Amount = evt.Amount;
        currentState.Status = "Created";
        
        state.Set(currentState);
        
        _logger.LogInformation("Order {OrderId} created for customer {CustomerId}", 
            evt.OrderId, evt.CustomerId);
    }

    private async Task HandleOrderSubmitted(OrderSubmitted evt, IStateContext state)
    {
        var currentState = state.Get<OrderProjection>();
        if (currentState != null)
        {
            currentState.Status = "Submitted";
            currentState.SubmittedAt = evt.SubmittedAt;
            state.Set(currentState);
        }
        
        _logger.LogInformation("Order {OrderId} submitted", evt.OrderId);
    }
}
```

### Stream Event Processor

```csharp
public class OrderStreamProcessor : IEventStreamProcessor
{
    public async ValueTask ConsumeAsync(
        StreamEvent evt,
        IStateContext state,
        bool hasMore,
        CancellationToken cancellationToken)
    {
        // Process events from specific streams
        if (evt.Metadata.StreamId.Category == "order")
        {
            await ProcessOrderEvent(evt, state);
        }
    }

    private async Task ProcessOrderEvent(StreamEvent evt, IStateContext state)
    {
        // Handle order-specific event processing
        // Update read models, trigger workflows, etc.
    }
}
```

## 🔧 Configuration

### Setting up EventStore

```csharp
// In your Startup.cs or Program.cs
services.AddChronicles(options =>
{
    options.AddDocumentStore("events", connectionString);
    
    options.AddEventStore("events", builder =>
    {
        builder.AddProcessor<OrderEventProcessor>();
        builder.AddStreamProcessor<OrderStreamProcessor>();
    });
});

// Register readers and writers
services.AddTransient<IEventStreamReader, EventStreamReader>();
services.AddTransient<IEventStreamWriter, EventStreamWriter>();
```

### Event Serialization

Chronicles automatically handles event serialization, but you can customize it:

```csharp
services.AddChronicles(options =>
{
    options.AddEventStore("events", builder =>
    {
        // Custom event name mapping
        builder.AddEvent<OrderCreated>("order.created.v1");
        builder.AddEvent<OrderUpdated>("order.updated.v2");
        
        // Custom serialization settings
        builder.ConfigureJsonSerialization(settings =>
        {
            settings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
            settings.NullValueHandling = NullValueHandling.Ignore;
        });
    });
});
```

## 📊 Performance Considerations

### Batching Events

```csharp
// ✅ Good: Write multiple related events together
var events = ImmutableList.Create<object>(
    new OrderCreated(orderId, customerId, amount),
    new OrderItemAdded(orderId, "product-1", 1, 50.00m),
    new OrderItemAdded(orderId, "product-2", 2, 25.00m)
);

await _eventWriter.WriteAsync(streamId, events);

// ❌ Avoid: Multiple separate writes
await _eventWriter.WriteAsync(streamId, ImmutableList.Create<object>(orderCreated));
await _eventWriter.WriteAsync(streamId, ImmutableList.Create<object>(firstItem));
await _eventWriter.WriteAsync(streamId, ImmutableList.Create<object>(secondItem));
```

### Stream Partitioning

```csharp
// Design stream IDs to distribute load evenly
var customerStreamId = new StreamId("customer", customerId);     // ✅ Good
var orderStreamId = new StreamId("order", $"{customerId}-{orderId}"); // ✅ Even better

// Avoid hot partitions
var globalStreamId = new StreamId("global", "single-stream");    // ❌ Avoid
```

## 🧪 Testing

Chronicles provides comprehensive testing utilities:

```csharp
[Test]
public async Task Should_Write_And_Read_Events()
{
    // Arrange
    var fakeWriter = new FakeEventStreamWriter();
    var fakeReader = new FakeEventStreamReader();
    
    var streamId = new StreamId("test", "123");
    var events = ImmutableList.Create<object>(
        new TestEventCreated("test-data")
    );

    // Act
    await fakeWriter.WriteAsync(streamId, events);

    // Assert
    var readEvents = await fakeReader.ReadAsync(streamId).ToListAsync();
    Assert.That(readEvents, Has.Count.EqualTo(1));
    Assert.That(readEvents[0].Data, Is.TypeOf<TestEventCreated>());
}
```

## 🚀 Next Steps

Now that you understand the EventStore layer:

- **[CQRS Layer](./cqrs-layer.md)** - Learn command and query patterns
- **[Testing Layer](./testing-layer.md)** - Master testing with Chronicles
- **[Best Practices](./best-practices.md)** - Optimization and patterns

---

> 💡 **Pro Tip**: Design your events to be immutable and backward-compatible. Once events are stored, they should never be changed - only new event types should be introduced for schema evolution.