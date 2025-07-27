# Best Practices

This guide covers recommended patterns, conventions, and practices for building robust, scalable applications with Chronicles.

## 🎯 General Principles

### Design for Events, Not Data

```csharp
// ✅ Good: Event represents business intent
public record CustomerRegistered(
    string CustomerId,
    string Email,
    string FullName,
    DateTime RegisteredAt,
    string RegistrationSource);

// ❌ Avoid: Generic data change events
public record CustomerDataChanged(
    string CustomerId,
    Dictionary<string, object> Changes);
```

### Keep Events Immutable and Small

```csharp
// ✅ Good: Focused, immutable event
public record OrderItemAdded(
    string OrderId,
    string ProductId,
    int Quantity,
    decimal UnitPrice);

// ❌ Avoid: Large, mutable events
public class OrderModified
{
    public string OrderId { get; set; }
    public List<OrderItem> AllItems { get; set; } // Entire state
    public CustomerInfo Customer { get; set; }     // Unrelated data
    public Dictionary<string, object> Metadata { get; set; } // Open-ended
}
```

### Use Explicit Command Naming

```csharp
// ✅ Good: Clear business intent
public record RegisterCustomer(string Email, string FullName);
public record ChangeCustomerEmail(string CustomerId, string NewEmail);
public record DeactivateCustomer(string CustomerId, string Reason);

// ❌ Avoid: Generic operations
public record UpdateCustomer(string CustomerId, CustomerData Data);
public record ModifyCustomer(string CustomerId, string Action, object Payload);
```

## 🏗️ Architecture Patterns

### Aggregate Design

Keep aggregates small and focused:

```csharp
// ✅ Good: Small, cohesive aggregate
public class OrderAggregate
{
    private string _orderId;
    private string _customerId;
    private List<OrderItem> _items = new();
    private OrderStatus _status;
    
    // Business methods that enforce invariants
    public void AddItem(string productId, int quantity, decimal price)
    {
        if (_status != OrderStatus.Created)
            throw new InvalidOperationException("Cannot modify submitted order");
            
        if (_items.Count >= 20)
            throw new InvalidOperationException("Maximum 20 items per order");
            
        // Emit event
        AddEvent(new OrderItemAdded(_orderId, productId, quantity, price));
    }
}

// ❌ Avoid: Large aggregates spanning multiple business concepts
public class CustomerOrderAccountAggregate // Too broad
{
    // Customer data
    // Order data  
    // Account data
    // Payment data
    // Shipping data
}
```

### Stream Naming Conventions

Use consistent, hierarchical naming:

```csharp
// ✅ Good: Consistent patterns
var orderStream = new StreamId("order", orderId);
var customerStream = new StreamId("customer", customerId);
var paymentStream = new StreamId("payment", paymentId);

// For hierarchical relationships
var orderItemStream = new StreamId("order-item", orderId, itemId);
var customerPreferenceStream = new StreamId("customer-preference", customerId, preferenceType);

// ❌ Avoid: Inconsistent or unclear names
var stream1 = new StreamId("ord", orderId);
var stream2 = new StreamId("CustomerData", customerId);
var stream3 = new StreamId("payment_info", paymentId);
```

### Command Handler Patterns

#### Single Responsibility

```csharp
// ✅ Good: Each handler does one thing
public class CreateOrderHandler : ICommandHandler<CreateOrder>
{
    public async ValueTask ExecuteAsync(ICommandContext<CreateOrder> context, CancellationToken cancellationToken)
    {
        // Only handles order creation
        var command = context.Command;
        
        // Validate
        if (context.State.Get<bool>("orderExists"))
            throw new InvalidOperationException("Order already exists");
            
        // Emit event
        context.AddEvent(new OrderCreated(command.OrderId, command.CustomerId, DateTime.UtcNow));
    }
}

// ❌ Avoid: Handlers that do multiple things
public class OrderManagementHandler : ICommandHandler<object> // Too generic
{
    public async ValueTask ExecuteAsync(ICommandContext<object> context, CancellationToken cancellationToken)
    {
        // Handles create, update, delete, etc. - violates SRP
    }
}
```

#### Validation Patterns

```csharp
public class AddOrderItemHandler : ICommandHandler<AddOrderItem, OrderState>
{
    public async ValueTask ExecuteAsync(
        ICommandContext<AddOrderItem> context, 
        OrderState state, 
        CancellationToken cancellationToken)
    {
        var command = context.Command;
        
        // ✅ Good: Clear validation with business reasons
        if (state.Status != OrderStatus.Created)
            throw new DomainException($"Cannot add items to {state.Status} order");
            
        if (command.Quantity <= 0)
            throw new ArgumentException("Quantity must be positive", nameof(command.Quantity));
            
        if (command.UnitPrice < 0)
            throw new ArgumentException("Price cannot be negative", nameof(command.UnitPrice));
            
        if (state.Items.Count >= MaxItemsPerOrder)
            throw new BusinessRuleException($"Order cannot exceed {MaxItemsPerOrder} items");
        
        // Business logic
        context.AddEvent(new OrderItemAdded(
            command.OrderId, 
            command.ProductId, 
            command.Quantity, 
            command.UnitPrice));
    }
    
    private const int MaxItemsPerOrder = 20;
}
```

## 📊 Event Design Patterns

### Event Schema Evolution

Design events for backward compatibility:

```csharp
// V1: Initial event
public record CustomerRegistered(
    string CustomerId,
    string Email,
    string Name);

// V2: Add optional field with default
public record CustomerRegistered(
    string CustomerId,
    string Email,
    string Name,
    string? PhoneNumber = null,
    string? PreferredLanguage = "en");

// V3: Breaking change - create new event
public record CustomerRegisteredV2(
    string CustomerId,
    string Email,
    PersonName Name,           // Complex type
    ContactInfo ContactInfo,   // New structure
    DateTime RegisteredAt);

// Handle both versions in projections
public OrderSummary? ConsumeEvent(StreamEvent evt, OrderSummary state)
{
    return evt.Data switch
    {
        CustomerRegistered v1 => ProjectV1(state, v1),
        CustomerRegisteredV2 v2 => ProjectV2(state, v2),
        _ => null
    };
}
```

### Event Metadata Usage

Leverage metadata for cross-cutting concerns:

```csharp
public class AuditEventProcessor : IEventProcessor
{
    public async ValueTask ConsumeAsync(
        StreamEvent evt, 
        IStateContext state, 
        bool hasMore, 
        CancellationToken cancellationToken)
    {
        // Use metadata for auditing
        var auditRecord = new AuditRecord
        {
            EventType = evt.Metadata.Name,
            StreamId = evt.Metadata.StreamId.ToString(),
            Version = evt.Metadata.Version.Value,
            Timestamp = evt.Metadata.Timestamp,
            CorrelationId = evt.Metadata.CorrelationId,
            CausationId = evt.Metadata.CausationId,
            EventData = JsonSerializer.Serialize(evt.Data)
        };
        
        await _auditService.RecordEventAsync(auditRecord, cancellationToken);
    }
}
```

## 🔍 Query Patterns

### Projection Design

Create projections tailored to specific use cases:

```csharp
// ✅ Good: Purpose-specific projections
public class OrderSummaryProjection : IStateProjection<OrderSummary>
{
    // Optimized for dashboard displays
    public OrderSummary CreateState(StreamId streamId) => new() { Id = streamId.Id };
    
    public OrderSummary? ConsumeEvent(StreamEvent evt, OrderSummary state)
    {
        return evt.Data switch
        {
            OrderCreated e => state with 
            { 
                CustomerId = e.CustomerId, 
                Status = "Created",
                CreatedAt = e.CreatedAt 
            },
            OrderItemAdded e => state with 
            { 
                ItemCount = state.ItemCount + 1,
                TotalAmount = state.TotalAmount + (e.Quantity * e.UnitPrice)
            },
            _ => null
        };
    }
}

public class OrderDetailProjection : IStateProjection<OrderDetail>
{
    // Optimized for order management screens
    public OrderDetail CreateState(StreamId streamId) => new() { Id = streamId.Id };
    
    public OrderDetail? ConsumeEvent(StreamEvent evt, OrderDetail state)
    {
        return evt.Data switch
        {
            OrderCreated e => state with 
            { 
                CustomerId = e.CustomerId,
                Status = OrderStatus.Created,
                CreatedAt = e.CreatedAt,
                Items = new List<OrderDetailItem>()
            },
            OrderItemAdded e => state with 
            { 
                Items = state.Items.Concat([new OrderDetailItem
                {
                    ProductId = e.ProductId,
                    Quantity = e.Quantity,
                    UnitPrice = e.UnitPrice,
                    LineTotal = e.Quantity * e.UnitPrice
                }]).ToList()
            },
            _ => null
        };
    }
}
```

### Document Projections for Persistence

```csharp
[ContainerName("order-summaries")]
public class OrderSummaryView : Document
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdated { get; set; }
    
    protected override string GetDocumentId() => OrderId;
    protected override string GetPartitionKey() => CustomerId;
}

public class OrderSummaryViewProjection : IDocumentProjection<OrderSummaryView>
{
    public OrderSummaryView CreateState(StreamId streamId)
    {
        return new OrderSummaryView { OrderId = streamId.Id };
    }

    public OrderSummaryView? ConsumeEvent(StreamEvent evt, OrderSummaryView state)
    {
        var updated = evt.Data switch
        {
            OrderCreated e => state with 
            { 
                CustomerId = e.CustomerId, 
                Status = "Created",
                CreatedAt = e.CreatedAt 
            },
            OrderItemAdded e => state with 
            { 
                ItemCount = state.ItemCount + 1,
                TotalAmount = state.TotalAmount + (e.Quantity * e.UnitPrice)
            },
            OrderSubmitted e => state with { Status = "Submitted" },
            _ => null
        };

        if (updated != null)
        {
            updated.LastUpdated = DateTime.UtcNow;
        }

        return updated;
    }

    public async ValueTask<DocumentCommitAction> OnCommitAsync(
        OrderSummaryView document, 
        CancellationToken cancellationToken)
    {
        // Always upsert for projections
        return DocumentCommitAction.Upsert;
    }
}
```

## 🔧 Performance Patterns

### Batch Event Processing

```csharp
// ✅ Good: Process events in batches
public class InventoryProjectionService
{
    private readonly List<StreamEvent> _eventBatch = new();
    private readonly Timer _batchTimer;
    
    public InventoryProjectionService()
    {
        _batchTimer = new Timer(ProcessBatch, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }

    public void EnqueueEvent(StreamEvent evt)
    {
        lock (_eventBatch)
        {
            _eventBatch.Add(evt);
            
            if (_eventBatch.Count >= 100) // Batch size threshold
            {
                ProcessBatch(null);
            }
        }
    }

    private async void ProcessBatch(object? state)
    {
        List<StreamEvent> batch;
        lock (_eventBatch)
        {
            if (_eventBatch.Count == 0) return;
            
            batch = new List<StreamEvent>(_eventBatch);
            _eventBatch.Clear();
        }

        await ProcessEventBatch(batch);
    }
}
```

### Optimize Cosmos DB Queries

```csharp
public class CustomerQueryService
{
    private readonly IDocumentReader<CustomerView> _reader;

    // ✅ Good: Partition-scoped queries
    public async Task<List<CustomerView>> GetTenantCustomersAsync(string tenantId)
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.TenantId = @tenantId AND c.Status = @status"
        )
        .WithParameter("@tenantId", tenantId)
        .WithParameter("@status", "Active");

        var results = new List<CustomerView>();
        await foreach (var customer in _reader.QueryAsync<CustomerView>(
            query, 
            partitionKey: tenantId)) // Partition-scoped
        {
            results.Add(customer);
        }
        
        return results;
    }

    // ❌ Avoid: Cross-partition queries when possible
    public async Task<List<CustomerView>> GetAllCustomersAsync()
    {
        var query = new QueryDefinition("SELECT * FROM c");
        
        var results = new List<CustomerView>();
        await foreach (var customer in _reader.QueryAsync<CustomerView>(
            query, 
            partitionKey: null)) // Cross-partition - expensive
        {
            results.Add(customer);
        }
        
        return results;
    }
}
```

### Event Store Partitioning

```csharp
// ✅ Good: Design partition keys for even distribution
public static class StreamIdFactory
{
    public static StreamId ForOrder(string orderId)
    {
        return new StreamId("order", orderId);
    }
    
    public static StreamId ForCustomer(string customerId)
    {
        return new StreamId("customer", customerId);
    }
    
    // For high-volume streams, consider hash-based distribution
    public static StreamId ForUserSession(string userId, string sessionId)
    {
        var hash = userId.GetHashCode() % 100; // Distribute across 100 partitions
        return new StreamId("session", $"{hash:00}", sessionId);
    }
}
```

## 🔒 Security Patterns

### Input Validation

```csharp
public class CreateOrderHandler : ICommandHandler<CreateOrder>
{
    private static readonly Regex OrderIdPattern = new(@"^[a-zA-Z0-9\-]+$");
    private static readonly Regex CustomerIdPattern = new(@"^cust_[a-zA-Z0-9]+$");

    public async ValueTask ExecuteAsync(
        ICommandContext<CreateOrder> context, 
        CancellationToken cancellationToken)
    {
        var command = context.Command;
        
        // ✅ Good: Strict input validation
        if (string.IsNullOrWhiteSpace(command.OrderId))
            throw new ArgumentException("Order ID is required");
            
        if (!OrderIdPattern.IsMatch(command.OrderId))
            throw new ArgumentException("Order ID contains invalid characters");
            
        if (command.OrderId.Length > 50)
            throw new ArgumentException("Order ID too long");
            
        if (!CustomerIdPattern.IsMatch(command.CustomerId))
            throw new ArgumentException("Invalid customer ID format");

        // Business logic...
    }
}
```

### Correlation and Causation IDs

```csharp
public class OrderController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() 
                           ?? Guid.NewGuid().ToString();
        
        var result = await _commandProcessor.ExecuteAsync(
            streamId: new StreamId("order", request.OrderId),
            command: new CreateOrder(request.OrderId, request.CustomerId),
            requestOptions: new CommandRequestOptions
            {
                CorrelationId = correlationId,
                CausationId = $"http-request-{HttpContext.TraceIdentifier}"
            },
            cancellationToken: HttpContext.RequestAborted);

        return Ok(result);
    }
}
```

## 🧪 Testing Patterns

### Test Data Builders

```csharp
public class OrderTestBuilder
{
    private readonly List<object> _events = new();
    private string _orderId = "order-123";
    private string _customerId = "customer-456";

    public OrderTestBuilder WithId(string orderId)
    {
        _orderId = orderId;
        return this;
    }

    public OrderTestBuilder WithCustomer(string customerId)
    {
        _customerId = customerId;
        return this;
    }

    public OrderTestBuilder Created()
    {
        _events.Add(new OrderCreated(_orderId, _customerId, DateTime.UtcNow));
        return this;
    }

    public OrderTestBuilder WithItem(string productId, int quantity, decimal price)
    {
        _events.Add(new OrderItemAdded(_orderId, productId, quantity, price));
        return this;
    }

    public OrderTestBuilder Submitted()
    {
        _events.Add(new OrderSubmitted(_orderId, DateTime.UtcNow));
        return this;
    }

    public List<object> BuildEvents() => new(_events);
    
    public StreamId GetStreamId() => new("order", _orderId);
}

// Usage in tests
[Test]
public async Task Should_Calculate_Order_Total_Correctly()
{
    // Arrange
    var testOrder = new OrderTestBuilder()
        .WithId("order-123")
        .WithCustomer("customer-456")
        .Created()
        .WithItem("product-1", 2, 25.00m)
        .WithItem("product-2", 1, 49.99m);

    _eventReader.AddEvents(testOrder.GetStreamId(), testOrder.BuildEvents());

    // Act
    var summary = await _queryService.GetOrderSummaryAsync("order-123");

    // Assert
    Assert.That(summary.TotalAmount, Is.EqualTo(99.99m));
}
```

### Integration Test Patterns

```csharp
public abstract class IntegrationTestBase
{
    protected FakeDocumentStoreProvider StoreProvider { get; private set; }
    protected IServiceProvider Services { get; private set; }

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
        
        // Configure Chronicles with fakes
        services.AddChronicles(options =>
        {
            StoreProvider = new FakeDocumentStoreProvider();
            options.AddFakeDocumentStore(StoreProvider);
        });
        
        // Add application services
        services.AddScoped<OrderService>();
        services.AddScoped<OrderQueryService>();
        
        Services = services.BuildServiceProvider();
    }

    protected T GetService<T>() where T : notnull => Services.GetRequiredService<T>();
}

public class OrderIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task Should_Complete_Order_Workflow()
    {
        // Arrange
        var orderService = GetService<OrderService>();
        var queryService = GetService<OrderQueryService>();

        // Act & Assert - Full workflow test
        await orderService.CreateOrderAsync("order-123", "customer-456");
        await orderService.AddItemAsync("order-123", "product-1", 2, 25.00m);
        await orderService.SubmitOrderAsync("order-123");
        
        var summary = await queryService.GetOrderSummaryAsync("order-123");
        Assert.That(summary.Status, Is.EqualTo("Submitted"));
    }
}
```

## 📈 Monitoring and Observability

### Structured Logging

```csharp
public class OrderEventProcessor : IEventProcessor
{
    private readonly ILogger<OrderEventProcessor> _logger;

    public async ValueTask ConsumeAsync(
        StreamEvent evt, 
        IStateContext state, 
        bool hasMore, 
        CancellationToken cancellationToken)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["StreamId"] = evt.Metadata.StreamId.ToString(),
            ["EventType"] = evt.Metadata.Name,
            ["Version"] = evt.Metadata.Version.Value,
            ["CorrelationId"] = evt.Metadata.CorrelationId
        });

        try
        {
            await ProcessEvent(evt, state, cancellationToken);
            
            _logger.LogInformation("Successfully processed {EventType} for {StreamId}",
                evt.Metadata.Name, evt.Metadata.StreamId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process {EventType} for {StreamId}",
                evt.Metadata.Name, evt.Metadata.StreamId);
            throw;
        }
    }
}
```

### Health Checks

```csharp
public class EventStoreHealthCheck : IHealthCheck
{
    private readonly IEventStreamWriter _eventWriter;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var testStreamId = new StreamId("health-check", Guid.NewGuid().ToString());
            var testEvent = new HealthCheckEvent(DateTime.UtcNow);
            
            await _eventWriter.WriteAsync(
                testStreamId, 
                ImmutableList.Create<object>(testEvent),
                cancellationToken: cancellationToken);
                
            return HealthCheckResult.Healthy("Event store is accessible");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Event store is not accessible", ex);
        }
    }
}

public record HealthCheckEvent(DateTime Timestamp);
```

## 🚀 Next Steps

Now that you understand the best practices:

- **[Performance Guide](./performance-guide.md)** - Optimization techniques
- **[Troubleshooting](./troubleshooting.md)** - Common issues and solutions
- **[Testing Layer](./testing-layer.md)** - Advanced testing patterns

---

> 💡 **Remember**: These are guidelines, not rigid rules. Adapt them to your specific domain and requirements while maintaining consistency within your codebase.