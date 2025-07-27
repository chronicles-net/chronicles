# CQRS Layer

The CQRS (Command Query Responsibility Segregation) layer in Chronicles provides a powerful framework for implementing command and query patterns with event sourcing. It separates write operations (commands) from read operations (queries), enabling scalable and maintainable architectures.

## 🎯 Core Concepts

### Commands vs Queries

**Commands** represent intentions to change state:
- Modify data
- Validate business rules  
- Emit events
- Return minimal data (success/failure)

**Queries** retrieve data without side effects:
- Read from projections
- Return rich data models
- No state changes
- Optimized for specific use cases

### CQRS Architecture in Chronicles

```
Commands → [Command Handlers] → Events → [Event Store]
                                    ↓
Queries ← [Projections] ← [Event Processors] ← Events
```

## 🎯 Commands and Command Handlers

### Simple Command Handler

Command handlers process commands and emit events:

```csharp
// Define your command
public record CreateOrder(string OrderId, string CustomerId, decimal Amount);

// Define your events
public record OrderCreated(string OrderId, string CustomerId, decimal Amount, DateTime CreatedAt);

// Create a stateless command handler
public class CreateOrderHandler : ICommandHandler<CreateOrder>
{
    public void ConsumeEvent(StreamEvent evt, CreateOrder command, IStateContext state)
    {
        // Optional: Track state during event replay
        switch (evt.Data)
        {
            case OrderCreated created:
                state.Set("orderExists", true);
                break;
        }
    }

    public async ValueTask ExecuteAsync(
        ICommandContext<CreateOrder> context, 
        CancellationToken cancellationToken)
    {
        var command = context.Command;
        
        // Business logic validation
        if (command.Amount <= 0)
            throw new ArgumentException("Order amount must be positive");
            
        // Check if order already exists
        if (context.State.Get<bool>("orderExists"))
            throw new InvalidOperationException("Order already exists");

        // Emit event
        var orderCreated = new OrderCreated(
            command.OrderId, 
            command.CustomerId, 
            command.Amount, 
            DateTime.UtcNow
        );
        
        context.AddEvent(orderCreated);
        
        // Optional: Set response
        context.Response = new { Success = true, OrderId = command.OrderId };
    }
}
```

### Stateful Command Handler

For complex business logic, use stateful handlers with projections:

```csharp
// Order aggregate state
public class OrderState
{
    public string Id { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = new();
    public OrderStatus Status { get; set; }
    public decimal Total => Items.Sum(i => i.Quantity * i.UnitPrice);
    public DateTime CreatedAt { get; set; }
}

// Add item command
public record AddOrderItem(string OrderId, string ProductId, int Quantity, decimal UnitPrice);
public record OrderItemAdded(string OrderId, string ProductId, int Quantity, decimal UnitPrice);

// Stateful command handler
public class AddOrderItemHandler : ICommandHandler<AddOrderItem, OrderState>
{
    public OrderState CreateState(StreamId streamId)
    {
        return new OrderState { Id = streamId.Id };
    }

    public OrderState? ConsumeEvent(StreamEvent evt, OrderState state)
    {
        return evt.Data switch
        {
            OrderCreated e => state with 
            { 
                Id = e.OrderId, 
                CustomerId = e.CustomerId, 
                Status = OrderStatus.Created,
                CreatedAt = e.CreatedAt
            },
            OrderItemAdded e => state with 
            { 
                Items = state.Items.Concat([new OrderItem(e.ProductId, e.Quantity, e.UnitPrice)]).ToList()
            },
            OrderSubmitted e => state with { Status = OrderStatus.Submitted },
            _ => null // No state change
        };
    }

    public async ValueTask ExecuteAsync(
        ICommandContext<AddOrderItem> context, 
        OrderState state, 
        CancellationToken cancellationToken)
    {
        var command = context.Command;
        
        // Business rule: Can only add items to created orders
        if (state.Status != OrderStatus.Created)
            throw new InvalidOperationException("Cannot add items to a submitted order");
            
        // Business rule: Maximum 10 items per order
        if (state.Items.Count >= 10)
            throw new InvalidOperationException("Maximum 10 items per order");

        // Emit event
        context.AddEvent(new OrderItemAdded(
            command.OrderId, 
            command.ProductId, 
            command.Quantity, 
            command.UnitPrice
        ));
        
        context.Response = new { Success = true, ItemCount = state.Items.Count + 1 };
    }
}
```

### Command Processing

Execute commands through the command processor:

```csharp
public class OrderService
{
    private readonly ICommandProcessor<CreateOrder> _createOrderProcessor;
    private readonly ICommandProcessor<AddOrderItem> _addItemProcessor;

    public OrderService(
        ICommandProcessor<CreateOrder> createOrderProcessor,
        ICommandProcessor<AddOrderItem> addItemProcessor)
    {
        _createOrderProcessor = createOrderProcessor;
        _addItemProcessor = addItemProcessor;
    }

    public async Task<CommandResult> CreateOrderAsync(string orderId, string customerId, decimal amount)
    {
        var streamId = new StreamId("order", orderId);
        var command = new CreateOrder(orderId, customerId, amount);
        
        var result = await _createOrderProcessor.ExecuteAsync(
            streamId: streamId,
            command: command,
            requestOptions: new CommandRequestOptions
            {
                CorrelationId = $"create-order-{Guid.NewGuid()}"
            },
            cancellationToken: CancellationToken.None
        );

        return result;
    }

    public async Task<CommandResult> AddItemAsync(string orderId, string productId, int quantity, decimal unitPrice)
    {
        var streamId = new StreamId("order", orderId);
        var command = new AddOrderItem(orderId, productId, quantity, unitPrice);
        
        return await _addItemProcessor.ExecuteAsync(streamId, command, null, CancellationToken.None);
    }
}
```

## 📊 State Projections

Projections build read models from events:

### Simple State Projection

```csharp
public class OrderSummaryProjection : IStateProjection<OrderSummary>
{
    public OrderSummary CreateState(StreamId streamId)
    {
        return new OrderSummary 
        { 
            Id = streamId.Id,
            Status = "New"
        };
    }

    public OrderSummary? ConsumeEvent(StreamEvent evt, OrderSummary state)
    {
        return evt.Data switch
        {
            OrderCreated e => state with
            {
                CustomerId = e.CustomerId,
                Status = "Created",
                CreatedAt = e.CreatedAt,
                LastUpdated = evt.Metadata.Timestamp
            },
            OrderItemAdded e => state with
            {
                ItemCount = state.ItemCount + 1,
                TotalAmount = state.TotalAmount + (e.Quantity * e.UnitPrice),
                LastUpdated = evt.Metadata.Timestamp
            },
            OrderSubmitted e => state with
            {
                Status = "Submitted",
                SubmittedAt = e.SubmittedAt,
                LastUpdated = evt.Metadata.Timestamp
            },
            _ => null
        };
    }
}

public record OrderSummary
{
    public string Id { get; init; } = string.Empty;
    public string CustomerId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int ItemCount { get; init; }
    public decimal TotalAmount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? SubmittedAt { get; init; }
    public DateTimeOffset LastUpdated { get; init; }
}
```

### Document Projections

For persistent read models, use document projections:

```csharp
[ContainerName("order-views")]
public class OrderView : Document
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<OrderViewItem> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }

    protected override string GetDocumentId() => OrderId;
    protected override string GetPartitionKey() => CustomerId;
}

public class OrderViewItem
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class OrderViewProjection : IDocumentProjection<OrderView>
{
    private readonly IDocumentReader<ProductDocument> _productReader;

    public OrderViewProjection(IDocumentReader<ProductDocument> productReader)
    {
        _productReader = productReader;
    }

    public OrderView CreateState(StreamId streamId)
    {
        return new OrderView { OrderId = streamId.Id };
    }

    public OrderView? ConsumeEvent(StreamEvent evt, OrderView state)
    {
        return evt.Data switch
        {
            OrderCreated e => state with
            {
                OrderId = e.OrderId,
                CustomerId = e.CustomerId,
                Status = "Created",
                CreatedAt = e.CreatedAt
            },
            OrderItemAdded e => AddItem(state, e),
            OrderSubmitted e => state with
            {
                Status = "Submitted",
                SubmittedAt = e.SubmittedAt
            },
            _ => null
        };
    }

    public async ValueTask<DocumentCommitAction> OnCommitAsync(
        OrderView document, 
        CancellationToken cancellationToken)
    {
        // Enrich with product names before saving
        foreach (var item in document.Items.Where(i => string.IsNullOrEmpty(i.ProductName)))
        {
            try
            {
                var product = await _productReader.ReadAsync<ProductDocument>(
                    item.ProductId, 
                    "products", 
                    null, 
                    cancellationToken: cancellationToken
                );
                item.ProductName = product.Name;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                item.ProductName = "Unknown Product";
            }
        }

        // Calculate total
        document.TotalAmount = document.Items.Sum(i => i.Quantity * i.UnitPrice);
        
        return DocumentCommitAction.Upsert;
    }

    private OrderView AddItem(OrderView state, OrderItemAdded evt)
    {
        var items = state.Items.ToList();
        items.Add(new OrderViewItem
        {
            ProductId = evt.ProductId,
            Quantity = evt.Quantity,
            UnitPrice = evt.UnitPrice
        });

        return state with { Items = items };
    }
}
```

## 🔍 Query Services

Build query services that read from projections:

```csharp
public class OrderQueryService
{
    private readonly IDocumentReader<OrderView> _orderViewReader;
    private readonly IEventStreamReader _eventReader;

    public OrderQueryService(
        IDocumentReader<OrderView> orderViewReader,
        IEventStreamReader eventReader)
    {
        _orderViewReader = orderViewReader;
        _eventReader = eventReader;
    }

    // Query from persistent projection
    public async Task<OrderView?> GetOrderViewAsync(string orderId, string customerId)
    {
        try
        {
            return await _orderViewReader.ReadAsync<OrderView>(orderId, customerId, null);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    // Query with filtering and pagination
    public async Task<PagedResult<OrderView>> GetCustomerOrdersAsync(
        string customerId, 
        string? status = null,
        int pageSize = 20,
        string? continuationToken = null)
    {
        var sql = "SELECT * FROM c WHERE c.CustomerId = @customerId";
        var query = new QueryDefinition(sql)
            .WithParameter("@customerId", customerId);

        if (!string.IsNullOrEmpty(status))
        {
            sql += " AND c.Status = @status";
            query = query.WithParameter("@status", status);
        }

        sql += " ORDER BY c.CreatedAt DESC";
        query = new QueryDefinition(sql);

        return await _orderViewReader.PagedQueryAsync<OrderView>(
            query: query,
            partitionKey: customerId,
            maxItemCount: pageSize,
            continuationToken: continuationToken
        );
    }

    // Real-time query by projecting events
    public async Task<OrderSummary> GetOrderSummaryAsync(string orderId)
    {
        var streamId = new StreamId("order", orderId);
        var projection = new OrderSummaryProjection();
        var summary = projection.CreateState(streamId);

        await foreach (var evt in _eventReader.ReadAsync(streamId))
        {
            var updated = projection.ConsumeEvent(evt, summary);
            if (updated != null)
                summary = updated;
        }

        return summary;
    }
}
```

## 🔄 Advanced Patterns

### Saga/Process Manager

Handle long-running business processes:

```csharp
public class OrderFulfillmentSaga : IEventProcessor
{
    private readonly ICommandProcessor<ReserveInventory> _reserveInventoryProcessor;
    private readonly ICommandProcessor<ProcessPayment> _processPaymentProcessor;

    public async ValueTask ConsumeAsync(
        StreamEvent evt, 
        IStateContext state, 
        bool hasMore, 
        CancellationToken cancellationToken)
    {
        switch (evt.Data)
        {
            case OrderSubmitted orderSubmitted:
                await HandleOrderSubmitted(orderSubmitted, state, cancellationToken);
                break;
                
            case InventoryReserved inventoryReserved:
                await HandleInventoryReserved(inventoryReserved, state, cancellationToken);
                break;
                
            case PaymentProcessed paymentProcessed:
                await HandlePaymentProcessed(paymentProcessed, state, cancellationToken);
                break;
        }
    }

    private async Task HandleOrderSubmitted(
        OrderSubmitted evt, 
        IStateContext state, 
        CancellationToken cancellationToken)
    {
        // Start fulfillment process
        var reserveCommand = new ReserveInventory(evt.OrderId, evt.Items);
        var inventoryStreamId = new StreamId("inventory", evt.OrderId);
        
        await _reserveInventoryProcessor.ExecuteAsync(
            inventoryStreamId, 
            reserveCommand, 
            null, 
            cancellationToken
        );
    }

    private async Task HandleInventoryReserved(
        InventoryReserved evt, 
        IStateContext state, 
        CancellationToken cancellationToken)
    {
        // Process payment after inventory is reserved
        var processPaymentCommand = new ProcessPayment(evt.OrderId, evt.Amount);
        var paymentStreamId = new StreamId("payment", evt.OrderId);
        
        await _processPaymentProcessor.ExecuteAsync(
            paymentStreamId, 
            processPaymentCommand, 
            null, 
            cancellationToken
        );
    }

    private async Task HandlePaymentProcessed(
        PaymentProcessed evt, 
        IStateContext state, 
        CancellationToken cancellationToken)
    {
        // Complete the order fulfillment
        // Ship the order, update status, etc.
    }
}
```

### Command Completion Handlers

React to command completion:

```csharp
public class OrderEmailService
{
    public async Task SetupCommandHandlers(ICommandProcessor<CreateOrder> processor)
    {
        // This is typically done during registration
        processor.RegisterCompletionHandler(async (command, result, context) =>
        {
            if (result.Result == ResultType.Success)
            {
                await SendOrderConfirmationEmail(command.CustomerId, command.OrderId);
            }
        });
    }

    private async Task SendOrderConfirmationEmail(string customerId, string orderId)
    {
        // Send email logic
    }
}
```

## 🔧 Configuration

### Registering Command Handlers

```csharp
// In your Startup.cs or Program.cs
services.AddChronicles(options =>
{
    options.AddDocumentStore("main", connectionString);
    
    options.AddCqrs(builder =>
    {
        // Register command handlers
        builder.AddCommandHandler<CreateOrder, CreateOrderHandler>();
        builder.AddCommandHandler<AddOrderItem, OrderState, AddOrderItemHandler>();
        
        // Register projections
        builder.AddProjection<OrderSummaryProjection, OrderSummary>();
        builder.AddDocumentProjection<OrderView, OrderViewProjection>();
        
        // Register processors
        builder.AddEventProcessor<OrderFulfillmentSaga>();
    });
});

// Register query services
services.AddTransient<OrderQueryService>();
```

### Command Options

```csharp
var result = await commandProcessor.ExecuteAsync(
    streamId: streamId,
    command: command,
    requestOptions: new CommandRequestOptions
    {
        CorrelationId = "web-request-123",
        CausationId = "user-action-456",
        Consistency = CommandConsistency.Strong,
        ConflictBehavior = CommandConflictBehavior.Retry,
        RetryAttempts = 3,
        Timeout = TimeSpan.FromSeconds(30)
    },
    cancellationToken: cancellationToken
);
```

## 🧪 Testing CQRS Components

### Testing Command Handlers

```csharp
[Test]
public async Task Should_Create_Order_Successfully()
{
    // Arrange
    var handler = new CreateOrderHandler();
    var context = new FakeCommandContext<CreateOrder>(
        new CreateOrder("order-123", "customer-456", 99.99m)
    );

    // Act
    await handler.ExecuteAsync(context, CancellationToken.None);

    // Assert
    Assert.That(context.Events, Has.Count.EqualTo(1));
    var orderCreated = context.Events[0] as OrderCreated;
    Assert.That(orderCreated.OrderId, Is.EqualTo("order-123"));
    Assert.That(orderCreated.Amount, Is.EqualTo(99.99m));
}
```

### Testing Projections

```csharp
[Test]
public void Should_Project_Order_Summary_Correctly()
{
    // Arrange
    var projection = new OrderSummaryProjection();
    var streamId = new StreamId("order", "123");
    var state = projection.CreateState(streamId);

    var events = new[]
    {
        new StreamEvent(new OrderCreated("123", "customer-1", 100m, DateTime.Now), EventMetadata.Empty),
        new StreamEvent(new OrderItemAdded("123", "product-1", 2, 25m), EventMetadata.Empty),
        new StreamEvent(new OrderSubmitted("123", DateTime.Now), EventMetadata.Empty)
    };

    // Act
    foreach (var evt in events)
    {
        var updated = projection.ConsumeEvent(evt, state);
        if (updated != null) state = updated;
    }

    // Assert
    Assert.That(state.Status, Is.EqualTo("Submitted"));
    Assert.That(state.ItemCount, Is.EqualTo(1));
    Assert.That(state.TotalAmount, Is.EqualTo(50m));
}
```

## 🚀 Next Steps

Now that you understand the CQRS layer:

- **[Testing Layer](./testing-layer.md)** - Learn comprehensive testing strategies
- **[Best Practices](./best-practices.md)** - CQRS optimization and patterns
- **[Performance Guide](./performance-guide.md)** - Scaling CQRS applications

---

> 💡 **Design Tip**: Keep commands focused and cohesive. Each command should represent a single business intent and maintain strong consistency within an aggregate boundary.