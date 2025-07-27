# Event Sourcing Primer

## 🎯 What is Event Sourcing?

Event Sourcing is an architectural pattern where instead of storing just the current state of data, we store the sequence of events that led to that state. Think of it like a bank statement - instead of just showing your current balance, it shows every transaction that contributed to that balance.

## 📚 Core Concepts

### Events as First-Class Citizens

In event sourcing, **events** are immutable facts that represent something that happened in your domain:

```csharp
// Instead of storing current state:
public class Order
{
    public string Id { get; set; }
    public OrderStatus Status { get; set; } // Only current state
    public decimal Total { get; set; }
}

// We store the events that created this state:
public record OrderCreated(string OrderId, DateTime CreatedAt, string CustomerId);
public record OrderItemAdded(string OrderId, string ProductId, int Quantity, decimal Price);
public record OrderShipped(string OrderId, DateTime ShippedAt, string TrackingNumber);
public record OrderDelivered(string OrderId, DateTime DeliveredAt);
```

### Event Streams

Events are organized into **streams**, typically one stream per aggregate or entity:

```
Order Stream: order-12345
├── OrderCreated      (version 1)
├── OrderItemAdded    (version 2) 
├── OrderItemAdded    (version 3)
├── OrderShipped      (version 4)
└── OrderDelivered    (version 5)
```

### State Reconstruction

Current state is reconstructed by replaying events from the beginning:

```csharp
public class OrderAggregate
{
    public string Id { get; private set; }
    public OrderStatus Status { get; private set; }
    public List<OrderItem> Items { get; private set; } = new();
    public decimal Total => Items.Sum(i => i.Price * i.Quantity);

    // Apply events to rebuild state
    public void Apply(OrderCreated evt)
    {
        Id = evt.OrderId;
        Status = OrderStatus.Created;
    }

    public void Apply(OrderItemAdded evt)
    {
        Items.Add(new OrderItem(evt.ProductId, evt.Quantity, evt.Price));
    }

    public void Apply(OrderShipped evt)
    {
        Status = OrderStatus.Shipped;
    }
}
```

## 🏗️ Architecture Overview

### Traditional CRUD vs Event Sourcing

#### Traditional CRUD Approach:
```
Command → [Business Logic] → Update Database → Response
   ↓
Current State Only (Previous states lost)
```

#### Event Sourcing Approach:
```
Command → [Business Logic] → Append Events → Response
   ↓
Event Stream → [Projection] → Current State (All history preserved)
```

### Key Components

#### 1. **Events** 
Immutable records of what happened:
```csharp
public record CustomerRegistered(
    string CustomerId,
    string Email,
    string Name,
    DateTime RegisteredAt
);
```

#### 2. **Event Store**
Persistent storage for events:
```csharp
// Chronicles EventStore
await eventWriter.WriteAsync(
    streamId: StreamId.From("customer", customerId),
    events: ImmutableList.Create<object>(customerRegistered)
);
```

#### 3. **Aggregates**
Domain objects that ensure business rules:
```csharp
public class Customer
{
    public void Register(string email, string name)
    {
        // Business rule validation
        if (string.IsNullOrEmpty(email))
            throw new ArgumentException("Email is required");
            
        // Emit event
        var evt = new CustomerRegistered(Id, email, name, DateTime.UtcNow);
        AddEvent(evt);
    }
}
```

#### 4. **Projections**
Read models built from events:
```csharp
public class CustomerProjection : IStateProjection<CustomerState>
{
    public CustomerState Project(CustomerState state, StreamEvent evt)
    {
        return evt.Data switch
        {
            CustomerRegistered e => state with { Name = e.Name, Email = e.Email },
            CustomerEmailChanged e => state with { Email = e.NewEmail },
            _ => state
        };
    }
}
```

## ✅ Benefits of Event Sourcing

### 🔍 **Complete Audit Trail**
Every change is recorded with full context:
```csharp
// You can answer questions like:
// - What was the order total on March 15th?
// - Who changed the customer's address?
// - How many times was this product returned?
```

### ⏱️ **Temporal Queries**
Query data as it existed at any point in time:
```csharp
// Get customer state as of specific date
var historicalState = await projectionService.ProjectAsOfAsync<CustomerState>(
    customerId, 
    asOf: DateTime.Parse("2024-03-15")
);
```

### 🔄 **Replay and Recovery**
Rebuild any read model from events:
```csharp
// Rebuild customer summary projection
await foreach (var evt in eventReader.ReadAsync(customerId))
{
    customerSummary = projection.Project(customerSummary, evt);
}
```

### 📊 **Multiple Views**
Create different projections for different needs:
```csharp
// Same events, different projections
CustomerDetailView    // For customer service
CustomerSummaryView   // For dashboards  
CustomerAnalyticsView // For reporting
```

### 🧪 **Testing**
Easy to test with known event sequences:
```csharp
[Test]
public void Should_Calculate_Total_Correctly()
{
    var aggregate = new OrderAggregate();
    
    // Given these events happened
    aggregate.Apply(new OrderCreated("order-1", DateTime.Now, "customer-1"));
    aggregate.Apply(new OrderItemAdded("order-1", "product-1", 2, 50.00m));
    aggregate.Apply(new OrderItemAdded("order-1", "product-2", 1, 25.00m));
    
    // Then the total should be correct
    Assert.That(aggregate.Total, Is.EqualTo(125.00m));
}
```

## ⚠️ Considerations and Challenges

### Schema Evolution
Events need to be backward compatible:
```csharp
// V1 Event
public record OrderCreated(string OrderId, string CustomerId);

// V2 Event (add field with default)
public record OrderCreated(string OrderId, string CustomerId, string? Currency = "USD");
```

### Eventual Consistency
Projections may be slightly behind events:
```csharp
// Command side: Events written immediately
await commandProcessor.ExecuteAsync(new CreateOrder(...));

// Query side: Projection may take time to update
var order = await queryService.GetOrderAsync(orderId); // Might not include latest changes yet
```

### Storage Growth
Events accumulate over time:
```csharp
// Strategies:
// 1. Snapshots for performance
// 2. Archiving old events
// 3. Event compression
```

## 🎯 When to Use Event Sourcing

### ✅ Great Fit For:
- **Audit Requirements**: Financial, healthcare, compliance domains
- **Complex Business Logic**: Rich domain models with many state transitions  
- **Analytics**: Need to understand how data changed over time
- **Debugging**: Want complete visibility into system behavior
- **Integration**: Publishing events to other systems

### ❌ Consider Alternatives For:
- **Simple CRUD**: Basic data entry applications
- **Read-Heavy Systems**: Where queries far outweigh commands
- **Small Teams**: Limited experience with event sourcing patterns
- **Legacy Integration**: Existing systems not designed for events

## 🚀 Getting Started with Chronicles

Now that you understand event sourcing fundamentals, you're ready to dive into Chronicles:

1. **[Getting Started Guide](./getting-started.md)** - Build your first event-sourced application
2. **[EventStore Layer](./eventstore-layer.md)** - Deep dive into event persistence
3. **[CQRS Layer](./cqrs-layer.md)** - Learn command and query patterns

## 📖 Further Reading

- [Martin Fowler on Event Sourcing](https://martinfowler.com/eaaDev/EventSourcing.html)
- [Event Sourcing Patterns](https://microservices.io/patterns/data/event-sourcing.html)
- [CQRS Journey by Microsoft](https://docs.microsoft.com/en-us/previous-versions/msp-n-p/jj554200(v=pandp.10))

---

> 💡 **Next Step**: Ready to implement? Check out the [Getting Started Guide](./getting-started.md) to build your first Chronicles application!