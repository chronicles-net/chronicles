# Testing with Chronicles

Chronicles provides `AddFakeChronicles` as a drop-in replacement for `AddChronicles` that uses in-memory storage instead of Cosmos DB. This makes testing fast, isolated, and dependency-free.

All test examples use **xUnit v3** with **FluentAssertions** and **AutoFixture** — the standard stack for Chronicles tests. Tests target `net10.0` and run on the same CI pipeline as production code.

## AddFakeChronicles

`AddFakeChronicles` provides the same API as `AddChronicles` but with in-memory implementations:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Chronicles.EventStore;
using Chronicles.Cqrs;

public class OrderServiceTests
{
    private readonly ServiceProvider provider;

    public OrderServiceTests()
    {
        var services = new ServiceCollection();

        services.AddFakeChronicles(store =>
        {
            store.WithEventStore(eventStore =>
            {
                eventStore.AddEvent<OrderPlaced>("order-placed:v1");
                eventStore.AddEvent<OrderShipped>("order-shipped:v1");
                eventStore.AddEvent<OrderCancelled>("order-cancelled:v1");
            })
            .WithCqrs(cqrs =>
            {
                cqrs.AddCommand<PlaceOrder, PlaceOrderHandler>();
                cqrs.AddCommand<ShipOrder, ShipOrderHandler, OrderState>();
            });
        });

        this.provider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task PlaceOrder_CreatesOrderStream()
    {
        // Arrange
        var processor = provider.GetRequiredService<ICommandProcessor<PlaceOrder>>();
        var reader = provider.GetRequiredService<IEventStreamReader>();

        var streamId = new StreamId("order", "ord-123");
        var command = new PlaceOrder("ord-123", "cust-456", 99.99m);

        // Act
        await processor.ExecuteAsync(streamId, command, null, default);

        // Assert
        var events = new List<object>();
        await foreach (var evt in reader.ReadAsync(streamId))
        {
            events.Add(evt.Data);
        }

        Assert.Single(events);
        var placed = Assert.IsType<OrderPlaced>(events[0]);
        Assert.Equal("ord-123", placed.OrderId);
        Assert.Equal("cust-456", placed.CustomerId);
        Assert.Equal(99.99m, placed.TotalAmount);
    }
}
```

## Testing Event Streams

### Writing and Reading Events

```csharp
[Fact]
public async Task EventStream_WriteAndRead()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddFakeChronicles(store =>
    {
        store.WithEventStore(eventStore =>
        {
            eventStore.AddEvent<OrderPlaced>("order-placed:v1");
            eventStore.AddEvent<OrderShipped>("order-shipped:v1");
        });
    });

    var provider = services.BuildServiceProvider();
    var writer = provider.GetRequiredService<IEventStreamWriter>();
    var reader = provider.GetRequiredService<IEventStreamReader>();

    var streamId = new StreamId("order", "ord-123");

    // Act
    await writer.WriteAsync(streamId, new[]
    {
        new OrderPlaced("ord-123", "cust-456", 99.99m, DateTimeOffset.UtcNow),
        new OrderShipped("ord-123", "TRACK-123", DateTimeOffset.UtcNow)
    }.ToImmutableList());

    // Assert
    var metadata = await reader.GetMetadataAsync(streamId);
    Assert.Equal(StreamState.Active, metadata.State);
    Assert.Equal(2L, (long)metadata.Version);

    var events = await reader.ReadAsync(streamId).ToListAsync();
    Assert.Equal(2, events.Count);
    Assert.IsType<OrderPlaced>(events[0].Data);
    Assert.IsType<OrderShipped>(events[1].Data);
}
```

### Testing Optimistic Concurrency

```csharp
[Fact]
public async Task EventStream_OptimisticConcurrency_ThrowsConflict()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddFakeChronicles(store =>
    {
        store.WithEventStore(eventStore =>
        {
            eventStore.AddEvent<OrderPlaced>("order-placed:v1");
        });
    });

    var provider = services.BuildServiceProvider();
    var writer = provider.GetRequiredService<IEventStreamWriter>();

    var streamId = new StreamId("order", "ord-123");

    await writer.WriteAsync(streamId, new[]
    {
        new OrderPlaced("ord-123", "cust-456", 99.99m, DateTimeOffset.UtcNow)
    }.ToImmutableList());

    // Act & Assert
    await Assert.ThrowsAsync<StreamConflictException>(async () =>
    {
        await writer.WriteAsync(
            streamId,
            new[] { new OrderPlaced("ord-123", "cust-789", 50m, DateTimeOffset.UtcNow) }.ToImmutableList(),
            new StreamWriteOptions
            {
                ExpectedVersion = StreamVersion.New // Expect stream to be new, but it's at version 1
            });
    });
}
```

### Testing Delete with Version Safety

```csharp
[Fact]
public async Task DeleteStream_WithExpectedVersion_ThrowsOnMismatch()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddFakeChronicles(store =>
    {
        store.WithEventStore(eventStore =>
        {
            eventStore.AddEvent<OrderPlaced>("order-placed:v1");
        });
    });

    var provider = services.BuildServiceProvider();
    var writer = provider.GetRequiredService<IEventStreamWriter>();

    var streamId = new StreamId("order", "ord-123");
    
    // Create stream at version 1
    await writer.WriteAsync(streamId, new[]
    {
        new OrderPlaced("ord-123", "cust-456", 99.99m, DateTimeOffset.UtcNow)
    }.ToImmutableList());

    // Act & Assert
    await Assert.ThrowsAsync<StreamConflictException>(async () =>
    {
        // Try to delete expecting version 0, but stream is at version 1
        await writer.DeleteStreamAsync(streamId, new StreamWriteOptions
        {
            ExpectedVersion = StreamVersion.New
        });
    });

    // This succeeds — deletes safely knowing we expect the current version
    await writer.DeleteStreamAsync(streamId, new StreamWriteOptions
    {
        ExpectedVersion = new StreamVersion(1)
    });
}
```

## Testing Command Handlers

### Stateless Command Handler Test

```csharp
[Fact]
public async Task PlaceOrderHandler_CreatesOrderPlacedEvent()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddFakeChronicles(store =>
    {
        store.WithEventStore(eventStore =>
        {
            eventStore.AddEvent<OrderPlaced>("order-placed:v1");
        })
        .WithCqrs(cqrs =>
        {
            cqrs.AddCommand<PlaceOrder, PlaceOrderHandler>();
        });
    });

    var provider = services.BuildServiceProvider();
    var processor = provider.GetRequiredService<ICommandProcessor<PlaceOrder>>();
    var reader = provider.GetRequiredService<IEventStreamReader>();

    var streamId = new StreamId("order", "ord-123");
    var command = new PlaceOrder("ord-123", "cust-456", 99.99m);

    // Act
    var result = await processor.ExecuteAsync(streamId, command, null, default);

    // Assert
    Assert.Equal(ResultType.Changed, result.Result);
    Assert.Equal(1L, (long)result.Version);

    var events = await reader.ReadAsync(streamId).ToListAsync();
    var placed = Assert.IsType<OrderPlaced>(events[0].Data);
    Assert.Equal("ord-123", placed.OrderId);
}
```

### Stateful Command Handler Test

```csharp
[Fact]
public async Task ShipOrderHandler_RequiresPlacedStatus()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddFakeChronicles(store =>
    {
        store.WithEventStore(eventStore =>
        {
            eventStore.AddEvent<OrderPlaced>("order-placed:v1");
            eventStore.AddEvent<OrderShipped>("order-shipped:v1");
            eventStore.AddEvent<OrderCancelled>("order-cancelled:v1");
        })
        .WithCqrs(cqrs =>
        {
            cqrs.AddCommand<PlaceOrder, PlaceOrderHandler>();
            cqrs.AddCommand<ShipOrder, ShipOrderHandler, OrderState>();
            cqrs.AddCommand<CancelOrder, CancelOrderHandler>();
        });
    });

    var provider = services.BuildServiceProvider();
    var placeProcessor = provider.GetRequiredService<ICommandProcessor<PlaceOrder>>();
    var shipProcessor = provider.GetRequiredService<ICommandProcessor<ShipOrder>>();
    var cancelProcessor = provider.GetRequiredService<ICommandProcessor<CancelOrder>>();

    var streamId = new StreamId("order", "ord-123");

    // Place order
    await placeProcessor.ExecuteAsync(streamId, new PlaceOrder("ord-123", "cust-456", 99.99m), null, default);

    // Cancel order
    await cancelProcessor.ExecuteAsync(streamId, new CancelOrder("ord-123", "Customer changed mind"), null, default);

    // Act & Assert - ship cancelled order should fail
    await Assert.ThrowsAsync<InvalidOperationException>(async () =>
    {
        await shipProcessor.ExecuteAsync(streamId, new ShipOrder("ord-123", "TRACK-123"), null, default);
    });
}
```

## Testing Document Store

### Writing and Reading Documents

```csharp
[Fact]
public async Task DocumentStore_WriteAndRead()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddFakeChronicles();

    var provider = services.BuildServiceProvider();
    var writer = provider.GetRequiredService<IDocumentWriter<OrderDocument>>();
    var reader = provider.GetRequiredService<IDocumentReader<OrderDocument>>();

    var document = new OrderDocument(
        Id: "ord-123",
        PartitionKey: "ord-123",
        CustomerId: "cust-456",
        TotalAmount: 99.99m,
        Status: OrderStatus.Placed,
        PlacedAt: DateTimeOffset.UtcNow);

    // Act
    await writer.WriteAsync(document);
    var read = await reader.ReadAsync<OrderDocument>("ord-123", "ord-123", null);

    // Assert
    Assert.Equal("ord-123", read.Id);
    Assert.Equal("cust-456", read.CustomerId);
    Assert.Equal(99.99m, read.TotalAmount);
}
```

### Testing Queries

```csharp
[Fact]
public async Task DocumentStore_Query()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddFakeChronicles();

    var provider = services.BuildServiceProvider();
    var writer = provider.GetRequiredService<IDocumentWriter<OrderDocument>>();
    var reader = provider.GetRequiredService<IDocumentReader<OrderDocument>>();

    await writer.WriteAsync(new OrderDocument("ord-1", "ord-1", "cust-1", 50m, OrderStatus.Placed, DateTimeOffset.UtcNow));
    await writer.WriteAsync(new OrderDocument("ord-2", "ord-2", "cust-1", 150m, OrderStatus.Placed, DateTimeOffset.UtcNow));
    await writer.WriteAsync(new OrderDocument("ord-3", "ord-3", "cust-2", 75m, OrderStatus.Placed, DateTimeOffset.UtcNow));

    // Act
    var query = new QueryDefinition("SELECT * FROM c WHERE c.customerId = @customerId")
        .WithParameter("@customerId", "cust-1");

    var results = await reader.QueryAsync<OrderDocument>(query, null, null).ToListAsync();

    // Assert
    Assert.Equal(2, results.Count);
    Assert.All(results, doc => Assert.Equal("cust-1", doc.CustomerId));
}
```

## Testing Projections

Test document projections by writing events and verifying projected documents. Projections subscribe to the event stream via the Cosmos DB change feed in production, but in-memory subscriptions in tests process events synchronously:

```csharp
[Fact]
public async Task Projection_UpdatesDocument()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddFakeChronicles(store =>
    {
        store.WithEventStore(eventStore =>
        {
            eventStore.AddEvent<OrderPlaced>("order-placed:v1");
            eventStore.AddEvent<OrderShipped>("order-shipped:v1");

            eventStore.AddEventSubscription("order-projection", subscription =>
            {
                subscription.MapAllStreams(processor =>
                {
                    processor.AddDocumentProjection<OrderDocument, OrderDocumentProjection>();
                });
            });
        });
    });

    var provider = services.BuildServiceProvider();
    var writer = provider.GetRequiredService<IEventStreamWriter>();
    var reader = provider.GetRequiredService<IDocumentReader<OrderDocument>>();

    var streamId = new StreamId("order", "ord-123");

    // Act - write events
    await writer.WriteAsync(streamId, new[]
    {
        new OrderPlaced("ord-123", "cust-456", 99.99m, DateTimeOffset.UtcNow),
        new OrderShipped("ord-123", "TRACK-123", DateTimeOffset.UtcNow)
    }.ToImmutableList());

    // Allow projection to process (in-memory is synchronous, but await for safety)
    await Task.Delay(50);

    // Assert - verify projected document
    var document = await reader.FindAsync("ord-123", "ord-123");
    Assert.NotNull(document);
    Assert.Equal("ord-123", document.Id);
    Assert.Equal("cust-456", document.CustomerId);
    Assert.Equal(OrderStatus.Shipped, document.Status);
    Assert.Equal("TRACK-123", document.TrackingNumber);
}
```

### Testing State Projection Rebuilding

State projections rebuild aggregate state by replaying events in order. Use this pattern to verify that your state projection correctly rebuilds from an event stream:

```csharp
[Fact]
public async Task StateProjection_RebuildsFromEventStream()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddFakeChronicles(store =>
    {
        store.WithEventStore(eventStore =>
        {
            eventStore.AddEvent<OrderPlaced>("order-placed:v1");
            eventStore.AddEvent<OrderShipped>("order-shipped:v1");
            eventStore.AddEvent<OrderCancelled>("order-cancelled:v1");
        })
        .WithCqrs(cqrs =>
        {
            cqrs.AddCommand<CancelOrder, CancelOrderHandler, OrderState>();
        });
    });

    var provider = services.BuildServiceProvider();
    var processor = provider.GetRequiredService<ICommandProcessor<CancelOrder>>();
    var reader = provider.GetRequiredService<IEventStreamReader>();

    var streamId = new StreamId("order", "ord-123");

    // Place and ship an order
    var writer = provider.GetRequiredService<IEventStreamWriter>();
    await writer.WriteAsync(streamId, new[]
    {
        new OrderPlaced("ord-123", "cust-456", 99.99m, DateTimeOffset.UtcNow),
        new OrderShipped("ord-123", "TRACK-123", DateTimeOffset.UtcNow)
    }.ToImmutableList());

    // Act - cancel order (verifies state projection replayed events correctly)
    var result = await processor.ExecuteAsync(
        streamId,
        new CancelOrder("ord-123", "Customer requested"),
        null,
        default);

    // Assert - state projection correctly identified shipped status and allowed cancellation
    Assert.Equal(ResultType.Changed, result.Result);
    
    var events = await reader.ReadAsync(streamId).ToListAsync();
    var cancelled = Assert.IsType<OrderCancelled>(events[^1].Data);
    Assert.Equal("Customer requested", cancelled.Reason);
}
```

## xUnit Integration

Chronicles works seamlessly with xUnit. Use class fixtures for shared setup:

```csharp
public class OrderTestFixture : IDisposable
{
    public ServiceProvider Provider { get; }

    public OrderTestFixture()
    {
        var services = new ServiceCollection();

        services.AddFakeChronicles(store =>
        {
            store.WithEventStore(eventStore =>
            {
                eventStore.AddEvent<OrderPlaced>("order-placed:v1");
                eventStore.AddEvent<OrderShipped>("order-shipped:v1");
                eventStore.AddEvent<OrderCancelled>("order-cancelled:v1");
            })
            .WithCqrs(cqrs =>
            {
                cqrs.AddCommand<PlaceOrder, PlaceOrderHandler>();
                cqrs.AddCommand<ShipOrder, ShipOrderHandler, OrderState>();
                cqrs.AddCommand<CancelOrder, CancelOrderHandler>();
            });
        });

        Provider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        Provider.Dispose();
    }
}

public class OrderCommandTests : IClassFixture<OrderTestFixture>
{
    private readonly OrderTestFixture fixture;

    public OrderCommandTests(OrderTestFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task PlaceOrder_Succeeds()
    {
        var processor = fixture.Provider.GetRequiredService<ICommandProcessor<PlaceOrder>>();
        var streamId = new StreamId("order", Guid.NewGuid().ToString());

        var result = await processor.ExecuteAsync(
            streamId,
            new PlaceOrder(streamId.Id, "cust-456", 99.99m),
            null,
            default);

        Assert.Equal(ResultType.Changed, result.Result);
    }
}
```

## Best Practices

- **Use `AddFakeChronicles` for all unit and integration tests** to avoid Cosmos DB dependencies
- **Use unique stream IDs** (e.g., `Guid.NewGuid().ToString()`) to isolate test cases
- **Test command handlers in isolation** — verify events produced, not document state
- **Test projections separately** — verify document transformations from events
- **Test state rebuilding** — verify that state projections correctly replay events in sequence
- **Use class fixtures in xUnit** for shared service provider setup
- **Keep tests focused** — test one behavior per test case
- **Verify both success and failure paths** for commands and projections
- **Test edge cases**: empty streams, null events, version conflicts, concurrent writes
- **Verify idempotency** when using `EventId` for deduplication scenarios

## API Changes in v1.0.0

### EventId for Idempotency

The `EventMetadata` record now includes an optional `EventId` property for deduplication and idempotency:

```csharp
// Write events with explicit IDs for deduplication
var eventId = Guid.NewGuid().ToString();
await writer.WriteAsync(streamId, new[]
{
    new OrderPlaced("ord-123", "cust-456", 99.99m, DateTimeOffset.UtcNow)
}, new StreamWriteOptions
{
    EventMetadata = new Dictionary<string, string> { ["EventId"] = eventId }
});
```

### IEventSubscriptionExceptionHandler Extended

Exception handlers now receive richer context for diagnostics:

```csharp
public class CustomExceptionHandler : IEventSubscriptionExceptionHandler
{
    private readonly ILogger<CustomExceptionHandler> logger;

    public CustomExceptionHandler(ILogger<CustomExceptionHandler> logger)
    {
        this.logger = logger;
    }

    public ValueTask HandleAsync(Exception exception, StreamEvent? streamEvent, CancellationToken cancellationToken)
    {
        if (streamEvent is not null)
        {
            logger.LogError(exception, "Error processing event {EventType} in stream {StreamId}",
                streamEvent.Metadata.Name,
                streamEvent.Metadata.StreamId);
        }
        else
        {
            logger.LogError(exception, "Error processing event subscription");
        }

        return ValueTask.CompletedTask;
    }
}
```

### CloseAsync for Permanent Stream Closure

Streams can now be closed to prevent further writes:

```csharp
[Fact]
public async Task CloseStream_PreventsWrites()
{
    var services = new ServiceCollection();
    services.AddFakeChronicles(store =>
    {
        store.WithEventStore(eventStore =>
        {
            eventStore.AddEvent<OrderPlaced>("order-placed:v1");
        });
    });

    var provider = services.BuildServiceProvider();
    var writer = provider.GetRequiredService<IEventStreamWriter>();

    var streamId = new StreamId("order", "ord-123");
    
    await writer.WriteAsync(streamId, new[]
    {
        new OrderPlaced("ord-123", "cust-456", 99.99m, DateTimeOffset.UtcNow)
    }.ToImmutableList());

    // Close the stream
    await writer.CloseAsync(streamId);

    // Verify stream is now closed
    var reader = provider.GetRequiredService<IEventStreamReader>();
    var metadata = await reader.GetMetadataAsync(streamId);
    Assert.Equal(StreamState.Closed, metadata.State);

    // Act & Assert - writes to closed stream throw
    await Assert.ThrowsAsync<StreamConflictException>(async () =>
    {
        await writer.WriteAsync(streamId, new[]
        {
            new OrderPlaced("ord-123", "cust-456", 49.99m, DateTimeOffset.UtcNow)
        }.ToImmutableList());
    });
}
```

## Testing Edge Cases

### Empty Streams

```csharp
[Fact]
public async Task EmptyStream_HasVersion0()
{
    var services = new ServiceCollection();
    services.AddFakeChronicles(store =>
    {
        store.WithEventStore(eventStore =>
        {
            eventStore.AddEvent<OrderPlaced>("order-placed:v1");
        });
    });

    var provider = services.BuildServiceProvider();
    var reader = provider.GetRequiredService<IEventStreamReader>();

    var streamId = new StreamId("order", "ord-123");

    // Non-existent stream
    var metadata = await reader.GetMetadataAsync(streamId);
    Assert.Equal(StreamVersion.New, metadata.Version);
    Assert.Equal(StreamState.New, metadata.State);
}
```

### Sentinel Version Values

`StreamVersion` provides sentinel values for concurrency guards:

```csharp
[Fact]
public async Task StreamVersion_Sentinels_Work()
{
    var services = new ServiceCollection();
    services.AddFakeChronicles(store =>
    {
        store.WithEventStore(eventStore =>
        {
            eventStore.AddEvent<OrderPlaced>("order-placed:v1");
        });
    });

    var provider = services.BuildServiceProvider();
    var writer = provider.GetRequiredService<IEventStreamWriter>();

    var streamId = new StreamId("order", "ord-123");

    // Write to new stream only (RequireEmpty guard)
    await writer.WriteAsync(streamId, new[]
    {
        new OrderPlaced("ord-123", "cust-456", 99.99m, DateTimeOffset.UtcNow)
    }.ToImmutableList(), new StreamWriteOptions
    {
        ExpectedVersion = StreamVersion.New
    });

    // Verify stream is no longer new
    var reader = provider.GetRequiredService<IEventStreamReader>();
    var metadata = await reader.GetMetadataAsync(streamId);
    Assert.NotEqual(StreamVersion.New, metadata.Version);

    // Write to existing stream only (RequireNotEmpty guard)
    await writer.WriteAsync(streamId, new[]
    {
        new OrderPlaced("ord-123", "cust-789", 50m, DateTimeOffset.UtcNow)
    }.ToImmutableList(), new StreamWriteOptions
    {
        ExpectedVersion = StreamVersion.RequireNotEmpty
    });

    // Verify second write succeeded
    var events = await reader.ReadAsync(streamId).ToListAsync();
    Assert.Equal(2, events.Count);
}
```

## Code Coverage

Chronicles maintains high test coverage via **XPlat Code Coverage** (Coverlet) in the CI pipeline. Coverage reports are generated on every push to `main` and badges are committed to `.github/coveragereport/`:

- **Branch Coverage Badge:** ![branch](../badge_branchcoverage.svg)
- **Line Coverage Badge:** ![line](../badge_linecoverage.svg)
- **Method Coverage Badge:** ![method](../badge_methodcoverage.svg)

All new tests must maintain or improve coverage. Coverage reports are available in the GitHub Actions workflow summary after each build.
