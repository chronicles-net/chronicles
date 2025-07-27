# Getting Started with Chronicles

This guide will walk you through building your first event-sourced application with Chronicles. We'll create a simple order management system that demonstrates all the key concepts and patterns.

## 🎯 What We'll Build

A basic order management system that supports:
- Creating orders
- Adding items to orders  
- Submitting orders
- Querying order history and status

## 📋 Prerequisites

- .NET 8.0 or later
- Azure Cosmos DB (or Cosmos DB Emulator for development)
- Basic understanding of C# and dependency injection

## 🚀 Step 1: Setup

### Create a New Project

```bash
dotnet new webapi -n OrderSystem
cd OrderSystem
```

### Install Chronicles

```bash
dotnet add package Chronicles
```

### Configure Chronicles

In `Program.cs`:

```csharp
using Chronicles.Documents.DependencyInjection;
using Chronicles.EventStore.DependencyInjection;
using Chronicles.Cqrs.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add Chronicles
builder.Services.AddChronicles(options =>
{
    // Configure Cosmos DB connection
    var connectionString = builder.Configuration.GetConnectionString("CosmosDb");
    options.AddDocumentStore("main", connectionString);
    
    // Configure EventStore
    options.AddEventStore("main", eventStore =>
    {
        eventStore.AddEvent<OrderCreated>("OrderCreated");
        eventStore.AddEvent<OrderItemAdded>("OrderItemAdded");
        eventStore.AddEvent<OrderSubmitted>("OrderSubmitted");
    });
    
    // Configure CQRS
    options.AddCqrs(cqrs =>
    {
        cqrs.AddCommandHandler<CreateOrder, CreateOrderHandler>();
        cqrs.AddCommandHandler<AddOrderItem, OrderState, AddOrderItemHandler>();
        cqrs.AddCommandHandler<SubmitOrder, OrderState, SubmitOrderHandler>();
        
        cqrs.AddProjection<OrderSummaryProjection, OrderSummary>();
    });
});

// Add application services
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<OrderQueryService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### Configuration

In `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "CosmosDb": "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

## 📝 Step 2: Define Events

Create the events that represent state changes in your domain:

```csharp
// Events/OrderEvents.cs
namespace OrderSystem.Events;

public record OrderCreated(
    string OrderId,
    string CustomerId,
    DateTime CreatedAt);

public record OrderItemAdded(
    string OrderId,
    string ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);

public record OrderSubmitted(
    string OrderId,
    DateTime SubmittedAt);
```

## 🎯 Step 3: Define Commands

Create commands that represent user intentions:

```csharp
// Commands/OrderCommands.cs
namespace OrderSystem.Commands;

public record CreateOrder(
    string OrderId,
    string CustomerId);

public record AddOrderItem(
    string OrderId,
    string ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);

public record SubmitOrder(
    string OrderId);
```

## 🏗️ Step 4: Create Domain State

Define the aggregate state that represents your business entity:

```csharp
// Domain/OrderState.cs
namespace OrderSystem.Domain;

public class OrderState
{
    public string Id { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = new();
    public OrderStatus Status { get; set; } = OrderStatus.Created;
    public DateTime CreatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    
    public decimal Total => Items.Sum(i => i.Quantity * i.UnitPrice);
    public int ItemCount => Items.Count;
}

public class OrderItem
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    
    public decimal Total => Quantity * UnitPrice;
}

public enum OrderStatus
{
    Created,
    Submitted,
    Shipped,
    Delivered,
    Cancelled
}
```

## ⚡ Step 5: Implement Command Handlers

Create handlers that process commands and emit events:

### Create Order Handler

```csharp
// Handlers/CreateOrderHandler.cs
using Chronicles.Cqrs;
using Chronicles.EventStore;
using OrderSystem.Commands;
using OrderSystem.Events;

namespace OrderSystem.Handlers;

public class CreateOrderHandler : ICommandHandler<CreateOrder>
{
    public void ConsumeEvent(StreamEvent evt, CreateOrder command, IStateContext state)
    {
        // Check if order already exists
        if (evt.Data is OrderCreated)
        {
            state.Set("orderExists", true);
        }
    }

    public async ValueTask ExecuteAsync(
        ICommandContext<CreateOrder> context,
        CancellationToken cancellationToken)
    {
        var command = context.Command;
        
        // Business rule: Order ID must not be empty
        if (string.IsNullOrEmpty(command.OrderId))
            throw new ArgumentException("Order ID is required");
            
        // Business rule: Customer ID must not be empty
        if (string.IsNullOrEmpty(command.CustomerId))
            throw new ArgumentException("Customer ID is required");
            
        // Business rule: Order must not already exist
        if (context.State.Get<bool>("orderExists"))
            throw new InvalidOperationException($"Order {command.OrderId} already exists");

        // Emit domain event
        var orderCreated = new OrderCreated(
            command.OrderId,
            command.CustomerId,
            DateTime.UtcNow);
            
        context.AddEvent(orderCreated);
        
        // Return success response
        context.Response = new { Success = true, OrderId = command.OrderId };
    }
}
```

### Add Item Handler  

```csharp
// Handlers/AddOrderItemHandler.cs
using Chronicles.Cqrs;
using Chronicles.EventStore;
using OrderSystem.Commands;
using OrderSystem.Domain;
using OrderSystem.Events;

namespace OrderSystem.Handlers;

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
                CreatedAt = e.CreatedAt,
                Status = OrderStatus.Created
            },
            OrderItemAdded e => AddItemToState(state, e),
            OrderSubmitted e => state with
            {
                Status = OrderStatus.Submitted,
                SubmittedAt = e.SubmittedAt
            },
            _ => null
        };
    }

    public async ValueTask ExecuteAsync(
        ICommandContext<AddOrderItem> context,
        OrderState state,
        CancellationToken cancellationToken)
    {
        var command = context.Command;
        
        // Business rules
        if (state.Status != OrderStatus.Created)
            throw new InvalidOperationException("Cannot add items to a submitted order");
            
        if (command.Quantity <= 0)
            throw new ArgumentException("Quantity must be positive");
            
        if (command.UnitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative");
            
        if (state.Items.Count >= 20) // Max 20 items per order
            throw new InvalidOperationException("Maximum 20 items per order");

        // Emit event
        var itemAdded = new OrderItemAdded(
            command.OrderId,
            command.ProductId,
            command.ProductName,
            command.Quantity,
            command.UnitPrice);
            
        context.AddEvent(itemAdded);
        
        context.Response = new 
        { 
            Success = true, 
            ItemCount = state.Items.Count + 1,
            NewTotal = state.Total + (command.Quantity * command.UnitPrice)
        };
    }

    private static OrderState AddItemToState(OrderState state, OrderItemAdded evt)
    {
        var items = state.Items.ToList();
        items.Add(new OrderItem
        {
            ProductId = evt.ProductId,
            ProductName = evt.ProductName,
            Quantity = evt.Quantity,
            UnitPrice = evt.UnitPrice
        });

        return state with { Items = items };
    }
}
```

### Submit Order Handler

```csharp
// Handlers/SubmitOrderHandler.cs
using Chronicles.Cqrs;
using Chronicles.EventStore;
using OrderSystem.Commands;
using OrderSystem.Domain;
using OrderSystem.Events;

namespace OrderSystem.Handlers;

public class SubmitOrderHandler : ICommandHandler<SubmitOrder, OrderState>
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
                CreatedAt = e.CreatedAt,
                Status = OrderStatus.Created
            },
            OrderItemAdded e => state with
            {
                Items = state.Items.Concat([new OrderItem
                {
                    ProductId = e.ProductId,
                    ProductName = e.ProductName,
                    Quantity = e.Quantity,
                    UnitPrice = e.UnitPrice
                }]).ToList()
            },
            OrderSubmitted e => state with
            {
                Status = OrderStatus.Submitted,
                SubmittedAt = e.SubmittedAt
            },
            _ => null
        };
    }

    public async ValueTask ExecuteAsync(
        ICommandContext<SubmitOrder> context,
        OrderState state,
        CancellationToken cancellationToken)
    {
        // Business rules
        if (state.Status != OrderStatus.Created)
            throw new InvalidOperationException("Order is not in Created status");
            
        if (state.Items.Count == 0)
            throw new InvalidOperationException("Cannot submit an order with no items");

        // Emit event
        var orderSubmitted = new OrderSubmitted(
            context.Command.OrderId,
            DateTime.UtcNow);
            
        context.AddEvent(orderSubmitted);
        
        context.Response = new 
        { 
            Success = true, 
            OrderId = context.Command.OrderId,
            Total = state.Total,
            ItemCount = state.Items.Count
        };
    }
}
```

## 📊 Step 6: Create Projections

Build read models for querying:

```csharp
// Projections/OrderSummaryProjection.cs
using Chronicles.Cqrs;
using Chronicles.EventStore;
using OrderSystem.Events;

namespace OrderSystem.Projections;

public class OrderSummaryProjection : IStateProjection<OrderSummary>
{
    public OrderSummary CreateState(StreamId streamId)
    {
        return new OrderSummary { Id = streamId.Id };
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
    public string Status { get; init; } = "New";
    public int ItemCount { get; init; }
    public decimal TotalAmount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? SubmittedAt { get; init; }
    public DateTimeOffset LastUpdated { get; init; }
}
```

## 🔧 Step 7: Create Application Services

### Order Service (Commands)

```csharp
// Services/OrderService.cs
using Chronicles.Cqrs;
using Chronicles.EventStore;
using OrderSystem.Commands;

namespace OrderSystem.Services;

public class OrderService
{
    private readonly ICommandProcessor<CreateOrder> _createOrderProcessor;
    private readonly ICommandProcessor<AddOrderItem> _addItemProcessor;
    private readonly ICommandProcessor<SubmitOrder> _submitOrderProcessor;

    public OrderService(
        ICommandProcessor<CreateOrder> createOrderProcessor,
        ICommandProcessor<AddOrderItem> addItemProcessor,
        ICommandProcessor<SubmitOrder> submitOrderProcessor)
    {
        _createOrderProcessor = createOrderProcessor;
        _addItemProcessor = addItemProcessor;
        _submitOrderProcessor = submitOrderProcessor;
    }

    public async Task<CommandResult> CreateOrderAsync(string orderId, string customerId)
    {
        var streamId = new StreamId("order", orderId);
        var command = new CreateOrder(orderId, customerId);
        
        return await _createOrderProcessor.ExecuteAsync(streamId, command, null, CancellationToken.None);
    }

    public async Task<CommandResult> AddItemAsync(
        string orderId, 
        string productId, 
        string productName, 
        int quantity, 
        decimal unitPrice)
    {
        var streamId = new StreamId("order", orderId);
        var command = new AddOrderItem(orderId, productId, productName, quantity, unitPrice);
        
        return await _addItemProcessor.ExecuteAsync(streamId, command, null, CancellationToken.None);
    }

    public async Task<CommandResult> SubmitOrderAsync(string orderId)
    {
        var streamId = new StreamId("order", orderId);
        var command = new SubmitOrder(orderId);
        
        return await _submitOrderProcessor.ExecuteAsync(streamId, command, null, CancellationToken.None);
    }
}
```

### Order Query Service

```csharp
// Services/OrderQueryService.cs
using Chronicles.EventStore;
using OrderSystem.Projections;

namespace OrderSystem.Services;

public class OrderQueryService
{
    private readonly IEventStreamReader _eventReader;
    private readonly OrderSummaryProjection _projection;

    public OrderQueryService(IEventStreamReader eventReader)
    {
        _eventReader = eventReader;
        _projection = new OrderSummaryProjection();
    }

    public async Task<OrderSummary> GetOrderSummaryAsync(string orderId)
    {
        var streamId = new StreamId("order", orderId);
        var summary = _projection.CreateState(streamId);

        await foreach (var evt in _eventReader.ReadAsync(streamId))
        {
            var updated = _projection.ConsumeEvent(evt, summary);
            if (updated != null)
                summary = updated;
        }

        return summary;
    }

    public async Task<List<StreamMetadata>> GetCustomerOrdersAsync(string customerId)
    {
        var streams = new List<StreamMetadata>();
        
        await foreach (var stream in _eventReader.QueryStreamsAsync(
            filter: $"STARTSWITH(c.streamId, 'order.')",
            createdAfter: DateTime.UtcNow.AddMonths(-6)))
        {
            streams.Add(stream);
        }
        
        return streams;
    }
}
```

## 🌐 Step 8: Create API Controllers

```csharp
// Controllers/OrdersController.cs
using Microsoft.AspNetCore.Mvc;
using OrderSystem.Services;

namespace OrderSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderService _orderService;
    private readonly OrderQueryService _queryService;

    public OrdersController(OrderService orderService, OrderQueryService queryService)
    {
        _orderService = orderService;
        _queryService = queryService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        try
        {
            var result = await _orderService.CreateOrderAsync(request.OrderId, request.CustomerId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("{orderId}/items")]
    public async Task<IActionResult> AddItem(string orderId, [FromBody] AddItemRequest request)
    {
        try
        {
            var result = await _orderService.AddItemAsync(
                orderId, 
                request.ProductId, 
                request.ProductName, 
                request.Quantity, 
                request.UnitPrice);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("{orderId}/submit")]
    public async Task<IActionResult> SubmitOrder(string orderId)
    {
        try
        {
            var result = await _orderService.SubmitOrderAsync(orderId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpGet("{orderId}")]
    public async Task<IActionResult> GetOrder(string orderId)
    {
        try
        {
            var summary = await _queryService.GetOrderSummaryAsync(orderId);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            return NotFound(new { Error = ex.Message });
        }
    }

    [HttpGet("customer/{customerId}")]
    public async Task<IActionResult> GetCustomerOrders(string customerId)
    {
        var orders = await _queryService.GetCustomerOrdersAsync(customerId);
        return Ok(orders);
    }
}

// Request DTOs
public record CreateOrderRequest(string OrderId, string CustomerId);

public record AddItemRequest(
    string ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);
```

## 🧪 Step 9: Add Tests

```csharp
// Tests/OrderServiceTests.cs
using Chronicles.Testing;
using OrderSystem.Services;
using OrderSystem.Events;

namespace OrderSystem.Tests;

public class OrderServiceTests
{
    private readonly FakeEventStreamReader _eventReader;
    private readonly FakeEventStreamWriter _eventWriter;
    private readonly OrderService _service;

    public OrderServiceTests()
    {
        _eventReader = new FakeEventStreamReader();
        _eventWriter = new FakeEventStreamWriter();
        
        // Set up command processors with fake dependencies
        var createProcessor = new FakeCommandProcessor<CreateOrder>();
        var addItemProcessor = new FakeCommandProcessor<AddOrderItem>();
        var submitProcessor = new FakeCommandProcessor<SubmitOrder>();
        
        _service = new OrderService(createProcessor, addItemProcessor, submitProcessor);
    }

    [Test]
    public async Task Should_Create_Order_Successfully()
    {
        // Arrange
        var orderId = "order-123";
        var customerId = "customer-456";

        // Act
        var result = await _service.CreateOrderAsync(orderId, customerId);

        // Assert
        Assert.That(result.Result, Is.EqualTo(ResultType.Success));
        Assert.That(result.Id.Id, Is.EqualTo(orderId));
    }
}
```

## 🚀 Step 10: Run the Application

```bash
# Start Cosmos DB Emulator (if using local development)
# Or ensure your Azure Cosmos DB is accessible

# Run the application
dotnet run

# Test the API
curl -X POST https://localhost:7000/api/orders \
  -H "Content-Type: application/json" \
  -d '{"orderId": "order-123", "customerId": "customer-456"}'

curl -X POST https://localhost:7000/api/orders/order-123/items \
  -H "Content-Type: application/json" \
  -d '{"productId": "product-1", "productName": "Widget", "quantity": 2, "unitPrice": 25.00}'

curl -X POST https://localhost:7000/api/orders/order-123/submit

curl -X GET https://localhost:7000/api/orders/order-123
```

## 🎉 What You've Accomplished

You've built a complete event-sourced application with Chronicles that:

✅ **Separates Commands and Queries** - Clear CQRS implementation  
✅ **Stores Complete Event History** - Full audit trail of all changes  
✅ **Enforces Business Rules** - Domain logic in command handlers  
✅ **Provides Multiple Views** - Projections for different read scenarios  
✅ **Is Fully Testable** - Unit tests without external dependencies  
✅ **Scales Horizontally** - Event sourcing enables distributed architectures

## 🔍 Next Steps

Now that you have a working application, explore:

1. **[Documents Layer](./documents-layer.md)** - Add persistent read models
2. **[EventStore Layer](./eventstore-layer.md)** - Advanced event patterns
3. **[CQRS Layer](./cqrs-layer.md)** - Complex business workflows
4. **[Testing Layer](./testing-layer.md)** - Comprehensive testing strategies
5. **[Best Practices](./best-practices.md)** - Production-ready patterns

## 🛠️ Troubleshooting

### Common Issues

**Connection Issues**
```bash
# Check Cosmos DB connection
dotnet run --urls="https://localhost:5001"
# Check logs for connection errors
```

**Event Serialization Issues**
```csharp
// Ensure events are JSON serializable
public record OrderCreated(string OrderId, string CustomerId, DateTime CreatedAt);
// Avoid: complex nested objects, circular references
```

**Command Handler Registration**
```csharp
// Ensure handlers are registered in Program.cs
options.AddCqrs(cqrs =>
{
    cqrs.AddCommandHandler<CreateOrder, CreateOrderHandler>();
    // Missing registrations will cause runtime errors
});
```

---

> 🎯 **Success!** You now have a solid foundation in Chronicles. Start building more complex features and explore the advanced patterns in the other documentation sections.