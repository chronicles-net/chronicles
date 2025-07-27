# Testing Layer

Chronicles provides comprehensive testing utilities that make it easy to unit test your event-sourced applications. The testing layer includes fake implementations of all major interfaces, allowing you to write fast, reliable tests without external dependencies.

## 🎯 Core Testing Concepts

### Test Doubles vs Real Implementations

Chronicles follows the **test double** pattern, providing fake implementations that:
- **Run in-memory** - No database connections required
- **Are deterministic** - Predictable behavior for consistent tests
- **Support inspection** - Access to internal state for assertions
- **Enable control** - Configure specific behaviors and responses

### Testing Layers

```
┌─────────────────┐
│   Your Tests    │
├─────────────────┤
│  Fake Objects   │  ← FakeDocumentReader, FakeEventStreamWriter, etc.
├─────────────────┤
│ Test Utilities  │  ← FakeDocumentStore, TestBuilders, etc.
├─────────────────┤
│   Chronicles    │  ← Real interfaces and contracts
└─────────────────┘
```

## 📚 Document Testing

### Setting Up Document Tests

```csharp
public class CustomerServiceTests
{
    private readonly FakeDocumentStoreProvider _storeProvider;
    private readonly FakeDocumentReader<CustomerDocument> _reader;
    private readonly FakeDocumentWriter<CustomerDocument> _writer;
    private readonly CustomerService _service;

    public CustomerServiceTests()
    {
        _storeProvider = new FakeDocumentStoreProvider();
        _reader = new FakeDocumentReader<CustomerDocument>(_storeProvider);
        _writer = new FakeDocumentWriter<CustomerDocument>(_storeProvider);
        _service = new CustomerService(_reader, _writer);
    }
}
```

### Testing Document Reading

```csharp
[Test]
public async Task Should_Return_Customer_When_Found()
{
    // Arrange
    var customerId = "customer-123";
    var tenantId = "tenant-1";
    var expectedCustomer = new CustomerDocument
    {
        Id = customerId,
        Name = "John Doe",
        Email = "john@example.com",
        TenantId = tenantId
    };

    // Pre-populate the fake store
    await _writer.CreateAsync(expectedCustomer);

    // Act
    var result = await _service.GetCustomerAsync(customerId, tenantId);

    // Assert
    Assert.That(result.Id, Is.EqualTo(customerId));
    Assert.That(result.Name, Is.EqualTo("John Doe"));
    Assert.That(result.Email, Is.EqualTo("john@example.com"));
}

[Test]
public async Task Should_Return_Null_When_Customer_Not_Found()
{
    // Arrange
    var customerId = "non-existent";
    var tenantId = "tenant-1";

    // Act & Assert
    var exception = await Assert.ThrowsAsync<CosmosException>(
        () => _service.GetCustomerAsync(customerId, tenantId)
    );
    
    Assert.That(exception.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
}
```

### Testing Document Writing

```csharp
[Test]
public async Task Should_Create_Customer_Successfully()
{
    // Arrange
    var name = "Jane Doe";
    var email = "jane@example.com";
    var tenantId = "tenant-1";

    // Act
    var result = await _service.CreateCustomerAsync(name, email, tenantId);

    // Assert
    Assert.That(result.Name, Is.EqualTo(name));
    Assert.That(result.Email, Is.EqualTo(email));
    Assert.That(result.TenantId, Is.EqualTo(tenantId));
    Assert.That(result.Id, Is.Not.Empty);

    // Verify it was actually written to the store
    var container = _storeProvider.GetStore().GetContainer<CustomerDocument>();
    var storedCustomer = await container.GetPartition(tenantId).GetDocumentAsync(result.Id);
    Assert.That(storedCustomer, Is.Not.Null);
    Assert.That(storedCustomer.Name, Is.EqualTo(name));
}

[Test]
public async Task Should_Update_Customer_Email()
{
    // Arrange
    var customer = new CustomerDocument
    {
        Id = "customer-123",
        Name = "John Doe",
        Email = "john@example.com",
        TenantId = "tenant-1"
    };
    await _writer.CreateAsync(customer);

    var newEmail = "john.doe@newcompany.com";

    // Act
    var result = await _service.UpdateCustomerEmailAsync(customer.Id, customer.TenantId, newEmail);

    // Assert
    Assert.That(result.Email, Is.EqualTo(newEmail));
    Assert.That(result.Name, Is.EqualTo("John Doe")); // Unchanged
}
```

### Testing Queries

```csharp
[Test]
public async Task Should_Find_Customers_By_Status()
{
    // Arrange
    var tenantId = "tenant-1";
    var activeCustomers = new[]
    {
        new CustomerDocument { Id = "1", Name = "John", Status = CustomerStatus.Active, TenantId = tenantId },
        new CustomerDocument { Id = "2", Name = "Jane", Status = CustomerStatus.Active, TenantId = tenantId }
    };
    var inactiveCustomer = new CustomerDocument 
    { 
        Id = "3", Name = "Bob", Status = CustomerStatus.Inactive, TenantId = tenantId 
    };

    // Populate test data
    foreach (var customer in activeCustomers.Concat(new[] { inactiveCustomer }))
    {
        await _writer.CreateAsync(customer);
    }

    // Configure query results for the fake reader
    _reader.QueryResults.Clear();
    _reader.QueryResults.AddRange(activeCustomers);

    // Act
    var results = await _service.GetActiveCustomersAsync(tenantId);

    // Assert
    Assert.That(results, Has.Count.EqualTo(2));
    Assert.That(results.All(c => c.Status == CustomerStatus.Active), Is.True);
}
```

## 🔄 EventStore Testing

### Setting Up EventStore Tests

```csharp
public class OrderEventTests
{
    private readonly FakeEventStreamReader _eventReader;
    private readonly FakeEventStreamWriter _eventWriter;
    private readonly OrderService _service;

    public OrderEventTests()
    {
        _eventReader = new FakeEventStreamReader();
        _eventWriter = new FakeEventStreamWriter();
        _service = new OrderService(_eventReader, _eventWriter);
    }
}
```

### Testing Event Writing

```csharp
[Test]
public async Task Should_Write_Order_Created_Event()
{
    // Arrange
    var orderId = "order-123";
    var customerId = "customer-456";
    var amount = 99.99m;

    // Act
    await _service.CreateOrderAsync(orderId, customerId, amount);

    // Assert
    var streamId = new StreamId("order", orderId);
    var writtenEvents = _eventWriter.GetWrittenEvents(streamId);
    
    Assert.That(writtenEvents, Has.Count.EqualTo(1));
    
    var orderCreated = writtenEvents[0] as OrderCreated;
    Assert.That(orderCreated, Is.Not.Null);
    Assert.That(orderCreated.OrderId, Is.EqualTo(orderId));
    Assert.That(orderCreated.CustomerId, Is.EqualTo(customerId));
    Assert.That(orderCreated.Amount, Is.EqualTo(amount));
}

[Test]
public async Task Should_Write_Multiple_Events_In_Sequence()
{
    // Arrange
    var orderId = "order-123";
    var streamId = new StreamId("order", orderId);

    // Act
    await _service.CreateOrderAsync(orderId, "customer-456", 99.99m);
    await _service.AddItemAsync(orderId, "product-1", 2, 25.00m);
    await _service.SubmitOrderAsync(orderId);

    // Assert
    var writtenEvents = _eventWriter.GetWrittenEvents(streamId);
    Assert.That(writtenEvents, Has.Count.EqualTo(3));
    
    Assert.That(writtenEvents[0], Is.TypeOf<OrderCreated>());
    Assert.That(writtenEvents[1], Is.TypeOf<OrderItemAdded>());
    Assert.That(writtenEvents[2], Is.TypeOf<OrderSubmitted>());
}
```

### Testing Event Reading

```csharp
[Test]
public async Task Should_Rebuild_Order_State_From_Events()
{
    // Arrange
    var orderId = "order-123";
    var streamId = new StreamId("order", orderId);
    
    var events = new object[]
    {
        new OrderCreated(orderId, "customer-456", 99.99m, DateTime.UtcNow),
        new OrderItemAdded(orderId, "product-1", 2, 25.00m),
        new OrderItemAdded(orderId, "product-2", 1, 49.99m),
        new OrderSubmitted(orderId, DateTime.UtcNow)
    };

    // Pre-populate events in the fake reader
    _eventReader.AddEvents(streamId, events);

    // Act
    var orderState = await _service.GetOrderStateAsync(orderId);

    // Assert
    Assert.That(orderState.Id, Is.EqualTo(orderId));
    Assert.That(orderState.Status, Is.EqualTo(OrderStatus.Submitted));
    Assert.That(orderState.Items, Has.Count.EqualTo(2));
    Assert.That(orderState.Total, Is.EqualTo(99.99m)); // 2 * 25.00 + 1 * 49.99
}

[Test]
public async Task Should_Handle_Empty_Stream()
{
    // Arrange
    var orderId = "non-existent-order";
    var streamId = new StreamId("order", orderId);

    // Act
    var orderState = await _service.GetOrderStateAsync(orderId);

    // Assert
    Assert.That(orderState.Id, Is.EqualTo(orderId));
    Assert.That(orderState.Status, Is.EqualTo(OrderStatus.New));
    Assert.That(orderState.Items, Is.Empty);
    Assert.That(orderState.Total, Is.EqualTo(0m));
}
```

### Testing Stream Metadata

```csharp
[Test]
public async Task Should_Return_Stream_Metadata()
{
    // Arrange
    var orderId = "order-123";
    var streamId = new StreamId("order", orderId);
    
    var events = new object[]
    {
        new OrderCreated(orderId, "customer-456", 99.99m, DateTime.UtcNow),
        new OrderSubmitted(orderId, DateTime.UtcNow)
    };

    _eventReader.AddEvents(streamId, events);

    // Act
    var metadata = await _eventReader.GetMetadataAsync(streamId);

    // Assert
    Assert.That(metadata.StreamId, Is.EqualTo(streamId));
    Assert.That(metadata.Version.Value, Is.EqualTo(2));
    Assert.That(metadata.State, Is.EqualTo(StreamState.Active));
}
```

## 🎯 CQRS Testing

### Testing Command Handlers

```csharp
public class CreateOrderHandlerTests
{
    private readonly CreateOrderHandler _handler;
    private readonly FakeCommandContext<CreateOrder> _context;

    public CreateOrderHandlerTests()
    {
        _handler = new CreateOrderHandler();
    }

    [Test]
    public async Task Should_Create_Order_Successfully()
    {
        // Arrange
        var command = new CreateOrder("order-123", "customer-456", 99.99m);
        var context = new FakeCommandContext<CreateOrder>(command);

        // Act
        await _handler.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.That(context.Events, Has.Count.EqualTo(1));
        
        var orderCreated = context.Events[0] as OrderCreated;
        Assert.That(orderCreated, Is.Not.Null);
        Assert.That(orderCreated.OrderId, Is.EqualTo("order-123"));
        Assert.That(orderCreated.Amount, Is.EqualTo(99.99m));
    }

    [Test]
    public async Task Should_Reject_Negative_Amount()
    {
        // Arrange
        var command = new CreateOrder("order-123", "customer-456", -10.00m);
        var context = new FakeCommandContext<CreateOrder>(command);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _handler.ExecuteAsync(context, CancellationToken.None)
        );
        
        Assert.That(exception.Message, Does.Contain("positive"));
        Assert.That(context.Events, Is.Empty);
    }

    [Test]
    public async Task Should_Prevent_Duplicate_Order_Creation()
    {
        // Arrange
        var command = new CreateOrder("order-123", "customer-456", 99.99m);
        var context = new FakeCommandContext<CreateOrder>(command);
        
        // Simulate existing order by setting state
        context.State.Set("orderExists", true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.ExecuteAsync(context, CancellationToken.None)
        );
        
        Assert.That(exception.Message, Does.Contain("already exists"));
        Assert.That(context.Events, Is.Empty);
    }
}
```

### Testing Stateful Command Handlers

```csharp
public class AddOrderItemHandlerTests
{
    private readonly AddOrderItemHandler _handler;

    public AddOrderItemHandlerTests()
    {
        _handler = new AddOrderItemHandler();
    }

    [Test]
    public async Task Should_Add_Item_To_Created_Order()
    {
        // Arrange
        var streamId = new StreamId("order", "order-123");
        var initialState = _handler.CreateState(streamId);
        
        // Apply initial events to build state
        var orderCreated = new OrderCreated("order-123", "customer-456", 0m, DateTime.UtcNow);
        var currentState = _handler.ConsumeEvent(
            new StreamEvent(orderCreated, EventMetadata.Empty), 
            initialState
        );

        var command = new AddOrderItem("order-123", "product-1", 2, 25.00m);
        var context = new FakeCommandContext<AddOrderItem>(command);

        // Act
        await _handler.ExecuteAsync(context, currentState!, CancellationToken.None);

        // Assert
        Assert.That(context.Events, Has.Count.EqualTo(1));
        
        var itemAdded = context.Events[0] as OrderItemAdded;
        Assert.That(itemAdded, Is.Not.Null);
        Assert.That(itemAdded.ProductId, Is.EqualTo("product-1"));
        Assert.That(itemAdded.Quantity, Is.EqualTo(2));
        Assert.That(itemAdded.UnitPrice, Is.EqualTo(25.00m));
    }

    [Test]
    public async Task Should_Reject_Adding_Item_To_Submitted_Order()
    {
        // Arrange
        var streamId = new StreamId("order", "order-123");
        var state = new OrderState
        {
            Id = "order-123",
            Status = OrderStatus.Submitted // Already submitted
        };

        var command = new AddOrderItem("order-123", "product-1", 2, 25.00m);
        var context = new FakeCommandContext<AddOrderItem>(command);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.ExecuteAsync(context, state, CancellationToken.None)
        );
        
        Assert.That(exception.Message, Does.Contain("submitted order"));
        Assert.That(context.Events, Is.Empty);
    }
}
```

### Testing Projections

```csharp
public class OrderSummaryProjectionTests
{
    private readonly OrderSummaryProjection _projection;

    public OrderSummaryProjectionTests()
    {
        _projection = new OrderSummaryProjection();
    }

    [Test]
    public void Should_Create_Initial_State()
    {
        // Arrange
        var streamId = new StreamId("order", "order-123");

        // Act
        var state = _projection.CreateState(streamId);

        // Assert
        Assert.That(state.Id, Is.EqualTo("order-123"));
        Assert.That(state.Status, Is.EqualTo("New"));
        Assert.That(state.ItemCount, Is.EqualTo(0));
        Assert.That(state.TotalAmount, Is.EqualTo(0m));
    }

    [Test]
    public void Should_Project_Order_Created_Event()
    {
        // Arrange
        var streamId = new StreamId("order", "order-123");
        var state = _projection.CreateState(streamId);
        
        var orderCreated = new OrderCreated("order-123", "customer-456", 99.99m, DateTime.UtcNow);
        var streamEvent = new StreamEvent(orderCreated, EventMetadata.Empty);

        // Act
        var updatedState = _projection.ConsumeEvent(streamEvent, state);

        // Assert
        Assert.That(updatedState, Is.Not.Null);
        Assert.That(updatedState.CustomerId, Is.EqualTo("customer-456"));
        Assert.That(updatedState.Status, Is.EqualTo("Created"));
        Assert.That(updatedState.CreatedAt, Is.EqualTo(orderCreated.CreatedAt));
    }

    [Test]
    public void Should_Project_Multiple_Events_Correctly()
    {
        // Arrange
        var streamId = new StreamId("order", "order-123");
        var state = _projection.CreateState(streamId);

        var events = new object[]
        {
            new OrderCreated("order-123", "customer-456", 0m, DateTime.UtcNow),
            new OrderItemAdded("order-123", "product-1", 2, 25.00m),
            new OrderItemAdded("order-123", "product-2", 1, 49.99m),
            new OrderSubmitted("order-123", DateTime.UtcNow)
        };

        // Act
        foreach (var evt in events)
        {
            var streamEvent = new StreamEvent(evt, EventMetadata.Empty);
            var updated = _projection.ConsumeEvent(streamEvent, state);
            if (updated != null) state = updated;
        }

        // Assert
        Assert.That(state.Status, Is.EqualTo("Submitted"));
        Assert.That(state.ItemCount, Is.EqualTo(2));
        Assert.That(state.TotalAmount, Is.EqualTo(99.99m)); // 2*25 + 1*49.99
        Assert.That(state.SubmittedAt, Is.Not.Null);
    }
}
```

## 🧪 Advanced Testing Patterns

### Integration-Style Tests with Fake Stores

```csharp
public class OrderWorkflowIntegrationTests
{
    private readonly FakeDocumentStoreProvider _storeProvider;
    private readonly OrderService _orderService;
    private readonly OrderQueryService _queryService;

    public OrderWorkflowIntegrationTests()
    {
        _storeProvider = new FakeDocumentStoreProvider();
        
        // Set up the full stack with fake implementations
        var eventReader = new FakeEventStreamReader();
        var eventWriter = new FakeEventStreamWriter();
        var documentReader = new FakeDocumentReader<OrderView>(_storeProvider);
        var documentWriter = new FakeDocumentWriter<OrderView>(_storeProvider);

        _orderService = new OrderService(eventReader, eventWriter);
        _queryService = new OrderQueryService(documentReader, eventReader);
    }

    [Test]
    public async Task Should_Complete_Full_Order_Workflow()
    {
        // Arrange
        var orderId = "order-123";
        var customerId = "customer-456";

        // Act - Create order
        await _orderService.CreateOrderAsync(orderId, customerId, 0m);
        
        // Act - Add items
        await _orderService.AddItemAsync(orderId, "product-1", 2, 25.00m);
        await _orderService.AddItemAsync(orderId, "product-2", 1, 49.99m);
        
        // Act - Submit order
        await _orderService.SubmitOrderAsync(orderId);

        // Act - Query the final state
        var summary = await _queryService.GetOrderSummaryAsync(orderId);

        // Assert
        Assert.That(summary.Id, Is.EqualTo(orderId));
        Assert.That(summary.CustomerId, Is.EqualTo(customerId));
        Assert.That(summary.Status, Is.EqualTo("Submitted"));
        Assert.That(summary.ItemCount, Is.EqualTo(2));
        Assert.That(summary.TotalAmount, Is.EqualTo(99.99m));
        Assert.That(summary.SubmittedAt, Is.Not.Null);
    }
}
```

### Testing Event Processors

```csharp
public class OrderEmailProcessorTests
{
    private readonly OrderEmailProcessor _processor;
    private readonly Mock<IEmailService> _emailService;

    public OrderEmailProcessorTests()
    {
        _emailService = new Mock<IEmailService>();
        _processor = new OrderEmailProcessor(_emailService.Object);
    }

    [Test]
    public async Task Should_Send_Email_When_Order_Created()
    {
        // Arrange
        var orderCreated = new OrderCreated("order-123", "customer-456", 99.99m, DateTime.UtcNow);
        var streamEvent = new StreamEvent(orderCreated, EventMetadata.Empty);
        var stateContext = new FakeStateContext();

        // Act
        await _processor.ConsumeAsync(streamEvent, stateContext, false, CancellationToken.None);

        // Assert
        _emailService.Verify(
            x => x.SendOrderConfirmationAsync("customer-456", "order-123", It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Test]
    public async Task Should_Not_Send_Email_For_Other_Events()
    {
        // Arrange
        var orderSubmitted = new OrderSubmitted("order-123", DateTime.UtcNow);
        var streamEvent = new StreamEvent(orderSubmitted, EventMetadata.Empty);
        var stateContext = new FakeStateContext();

        // Act
        await _processor.ConsumeAsync(streamEvent, stateContext, false, CancellationToken.None);

        // Assert
        _emailService.Verify(
            x => x.SendOrderConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }
}
```

## 🔧 Test Configuration and Setup

### Custom Test Attributes

```csharp
// Use AutoFixture with NSubstitute for automatic test data generation
[AttributeUsage(AttributeTargets.Method)]
public class AutoNSubstituteDataAttribute : AutoDataAttribute
{
    public AutoNSubstituteDataAttribute() 
        : base(() => new Fixture().Customize(new AutoNSubstituteCustomization()))
    {
    }
}

// Example usage
[Theory, AutoNSubstituteData]
public async Task Should_Process_Command_With_Generated_Data(
    CreateOrder command,           // Auto-generated
    ICommandProcessor<CreateOrder> processor)  // Auto-mocked
{
    // Test implementation
}
```

### Test Base Classes

```csharp
public abstract class DocumentTestBase<T> where T : class, IDocument
{
    protected FakeDocumentStoreProvider StoreProvider { get; }
    protected FakeDocumentReader<T> Reader { get; }
    protected FakeDocumentWriter<T> Writer { get; }

    protected DocumentTestBase()
    {
        StoreProvider = new FakeDocumentStoreProvider();
        Reader = new FakeDocumentReader<T>(StoreProvider);
        Writer = new FakeDocumentWriter<T>(StoreProvider);
    }

    protected async Task<T> CreateTestDocument(T document)
    {
        return await Writer.CreateAsync(document);
    }

    protected FakeContainer GetContainer()
    {
        return StoreProvider.GetStore().GetContainer<T>();
    }
}

// Usage
public class CustomerServiceTests : DocumentTestBase<CustomerDocument>
{
    private readonly CustomerService _service;

    public CustomerServiceTests()
    {
        _service = new CustomerService(Reader, Writer);
    }

    [Test]
    public async Task Should_Create_Customer()
    {
        // Test uses inherited Reader and Writer
        var customer = await _service.CreateCustomerAsync("John", "john@example.com", "tenant-1");
        Assert.That(customer.Name, Is.EqualTo("John"));
    }
}
```

## 📊 Testing Best Practices

### Arrange-Act-Assert Pattern

```csharp
[Test]
public async Task Should_Follow_AAA_Pattern()
{
    // Arrange - Set up test data and dependencies
    var orderId = "order-123";
    var customerId = "customer-456";
    var command = new CreateOrder(orderId, customerId, 99.99m);
    var context = new FakeCommandContext<CreateOrder>(command);

    // Act - Execute the operation being tested
    await _handler.ExecuteAsync(context, CancellationToken.None);

    // Assert - Verify the results
    Assert.That(context.Events, Has.Count.EqualTo(1));
    var orderCreated = context.Events[0] as OrderCreated;
    Assert.That(orderCreated.OrderId, Is.EqualTo(orderId));
}
```

### Test Data Builders

```csharp
public class OrderTestDataBuilder
{
    private string _orderId = "order-123";
    private string _customerId = "customer-456";
    private decimal _amount = 99.99m;
    private OrderStatus _status = OrderStatus.Created;
    private List<OrderItem> _items = new();

    public OrderTestDataBuilder WithId(string orderId)
    {
        _orderId = orderId;
        return this;
    }

    public OrderTestDataBuilder WithCustomer(string customerId)
    {
        _customerId = customerId;
        return this;
    }

    public OrderTestDataBuilder WithItem(string productId, int quantity, decimal price)
    {
        _items.Add(new OrderItem(productId, quantity, price));
        return this;
    }

    public OrderTestDataBuilder WithStatus(OrderStatus status)
    {
        _status = status;
        return this;
    }

    public OrderState BuildState()
    {
        return new OrderState
        {
            Id = _orderId,
            CustomerId = _customerId,
            Status = _status,
            Items = _items
        };
    }

    public List<object> BuildEvents()
    {
        var events = new List<object>
        {
            new OrderCreated(_orderId, _customerId, _amount, DateTime.UtcNow)
        };

        events.AddRange(_items.Select(item => 
            new OrderItemAdded(_orderId, item.ProductId, item.Quantity, item.UnitPrice)));

        if (_status == OrderStatus.Submitted)
        {
            events.Add(new OrderSubmitted(_orderId, DateTime.UtcNow));
        }

        return events;
    }
}

// Usage
[Test]
public async Task Should_Handle_Complex_Order_Scenario()
{
    // Arrange
    var events = new OrderTestDataBuilder()
        .WithId("order-123")
        .WithCustomer("customer-456")
        .WithItem("product-1", 2, 25.00m)
        .WithItem("product-2", 1, 49.99m)
        .WithStatus(OrderStatus.Submitted)
        .BuildEvents();

    var streamId = new StreamId("order", "order-123");
    _eventReader.AddEvents(streamId, events);

    // Act & Assert
    var state = await _service.GetOrderStateAsync("order-123");
    Assert.That(state.Status, Is.EqualTo(OrderStatus.Submitted));
    Assert.That(state.Items, Has.Count.EqualTo(2));
}
```

## 🚀 Next Steps

Now that you understand Chronicles testing capabilities:

- **[Best Practices](./best-practices.md)** - Learn optimization patterns and conventions
- **[Performance Guide](./performance-guide.md)** - Testing performance scenarios
- **[Getting Started](./getting-started.md)** - Build your first Chronicles application

---

> 💡 **Testing Philosophy**: Chronicles encourages testing business logic in isolation using fast, deterministic unit tests. Integration tests should focus on configuration and cross-cutting concerns rather than business rules.