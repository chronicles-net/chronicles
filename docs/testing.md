# Testing with Chronicles

Chronicles provides `AddFakeChronicles` as a drop-in replacement for `AddChronicles` that uses in-memory storage instead of Cosmos DB. This makes testing fast, isolated, and dependency-free.

## AddFakeChronicles

`AddFakeChronicles` provides the same API as `AddChronicles` but with in-memory implementations:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Chronicles.EventStore;
using Chronicles.Cqrs;

public class OrderServiceTests
{
    private readonly ServiceProvider _provider;

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

        _provider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task PlaceOrder_CreatesOrderStream()
    {
        // Arrange
        var processor = _provider.GetRequiredService<ICommandProcessor<PlaceOrder>>();
        var reader = _provider.GetRequiredService<IEventStreamReader>();

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

Test document projections by writing events and verifying projected documents:

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

    // Allow projection to process (in-memory is synchronous)
    await Task.Delay(100);

    // Assert - verify projected document
    var document = await reader.FindAsync("ord-123", "ord-123");
    Assert.NotNull(document);
    Assert.Equal("ord-123", document.Id);
    Assert.Equal("cust-456", document.CustomerId);
    Assert.Equal(OrderStatus.Shipped, document.Status);
    Assert.Equal("TRACK-123", document.TrackingNumber);
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
    private readonly OrderTestFixture _fixture;

    public OrderCommandTests(OrderTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PlaceOrder_Succeeds()
    {
        var processor = _fixture.Provider.GetRequiredService<ICommandProcessor<PlaceOrder>>();
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
- **Use class fixtures in xUnit** for shared service provider setup
- **Keep tests focused** — test one behavior per test case
- **Verify both success and failure paths** for commands and projections
