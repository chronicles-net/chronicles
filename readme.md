# Chronicles

[![Branch Coverage](.github/coveragereport/badge_branchcoverage.svg?raw=true)](https://github.com/chronicles-net/chronicles/actions/workflows/ci.yml)
[![Line Coverage](.github/coveragereport/badge_linecoverage.svg?raw=true)](https://github.com/chronicles-net/chronicles/actions/workflows/ci.yml)
[![Method Coverage](.github/coveragereport/badge_methodcoverage.svg?raw=true)](https://github.com/chronicles-net/chronicles/actions/workflows/ci.yml)

A comprehensive .NET library for **Event Sourcing** and **CQRS** (Command Query Responsibility Segregation) built on top of Azure Cosmos DB. Chronicles provides a robust foundation for building event-driven applications with strong consistency guarantees and powerful querying capabilities.

## Features

- 🎯 **Event Sourcing**: Store and replay events with full audit trails
- ⚡ **CQRS**: Separate command and query responsibilities for optimal performance
- 📊 **Document Projections**: Build read models from event streams
- 🔄 **State Management**: Reliable state management with checkpointing
- 🏗️ **Azure Cosmos DB**: Leverages Cosmos DB for global distribution and consistency
- 🧪 **Testing Support**: Comprehensive testing utilities for event sourcing scenarios
- 🚀 **High Performance**: Optimized for throughput and low latency

## Table of Contents

- [Installation](#installation)
- [Quick Start](#quick-start)
- [Core Concepts](#core-concepts)
- [API Overview](#api-overview)
- [Configuration](#configuration)
- [Usage Examples](#usage-examples)
- [Sample Applications](#sample-applications)
- [Testing](#testing)
- [Contributing](#contributing)
- [License](#license)

## Installation

Install the Chronicles package via NuGet:

```bash
dotnet add package Chronicles
```

Or via Package Manager Console:

```powershell
Install-Package Chronicles
```

### Prerequisites

- .NET 9.0 or later
- Azure Cosmos DB account

## Quick Start

Here's a simple example to get you started with Chronicles:

```csharp
using Chronicles.EventStore;
using Chronicles.Cqrs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Configure services
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddChronicles(documentStore => documentStore
    .WithEventStore(eventStore => eventStore
        .WithCqrs(cqrs => cqrs
            .AddCommandHandler<CreateOrderCommand, OrderState, CreateOrderHandler>())));

var app = builder.Build();

// Use the event store
var eventWriter = app.Services.GetRequiredService<IEventStreamWriter>();
var streamId = new StreamId("orders", "order-123");

// Write events
await eventWriter.WriteAsync(streamId, new[]
{
    new OrderCreated { OrderId = "order-123", CustomerId = "customer-456" },
    new OrderItemAdded { OrderId = "order-123", ProductId = "product-789", Quantity = 2 }
});
```

## Core Concepts

### Event Streams

Events are stored in streams identified by a `StreamId` consisting of a category and unique identifier:

```csharp
var streamId = new StreamId("orders", "order-123");
// Results in stream: "orders.order-123"
```

### Commands and Handlers

Commands represent intentions to change state, processed by command handlers:

```csharp
public record CreateOrderCommand(string OrderId, string CustomerId);

public class CreateOrderHandler : ICommandHandler<CreateOrderCommand, OrderState>
{
    public async ValueTask ExecuteAsync(
        ICommandContext<CreateOrderCommand> context,
        OrderState state,
        CancellationToken cancellationToken)
    {
        // Validate command
        if (state.IsCreated)
            throw new InvalidOperationException("Order already exists");

        // Emit events
        await context.EmitAsync(new OrderCreated 
        { 
            OrderId = context.Command.OrderId,
            CustomerId = context.Command.CustomerId 
        });
    }
}
```

### Event Processing

Process events as they occur using event processors:

```csharp
public class OrderProjection : IEventProcessor
{
    public async Task ProcessAsync(StreamEvent streamEvent, CancellationToken cancellationToken)
    {
        switch (streamEvent.Data)
        {
            case OrderCreated created:
                // Update read model
                await UpdateOrderSummary(created);
                break;
        }
    }
}
```

## API Overview

### Event Store Operations

```csharp
// Reading events
var events = eventReader.ReadAsync(streamId);
await foreach (var evt in events)
{
    // Process event
    Console.WriteLine($"Event: {evt.Type}, Data: {evt.Data}");
}

// Writing events
var result = await eventWriter.WriteAsync(streamId, new[]
{
    new ProductAdded { ProductId = "123", Name = "Widget" }
});

// Querying streams
var streams = eventReader.QueryStreamsAsync("orders.*");
await foreach (var stream in streams)
{
    Console.WriteLine($"Stream: {stream.StreamId}, Version: {stream.Version}");
}
```

### Command Execution

```csharp
// Execute commands
var commandProcessor = serviceProvider.GetRequiredService<ICommandProcessor>();

var result = await commandProcessor.ExecuteAsync(
    new CreateOrderCommand("order-123", "customer-456"));

if (result.IsSuccess)
{
    Console.WriteLine($"Command executed successfully");
}
```

### Document Projections

```csharp
public class OrderSummaryProjection : IDocumentProjection<OrderSummary>
{
    public string GetDocumentId(StreamEvent streamEvent)
        => streamEvent.StreamId.Id; // Use order ID as document ID

    public async Task<OrderSummary> ProjectAsync(
        OrderSummary? document,
        StreamEvent streamEvent,
        CancellationToken cancellationToken)
    {
        return streamEvent.Data switch
        {
            OrderCreated created => new OrderSummary
            {
                OrderId = created.OrderId,
                CustomerId = created.CustomerId,
                Status = "Created",
                CreatedAt = streamEvent.Timestamp
            },
            OrderShipped shipped => document with { Status = "Shipped" },
            _ => document
        };
    }
}
```

## Configuration

### Azure Cosmos DB Setup

Configure your Azure Cosmos DB connection:

```csharp
builder.Services.AddChronicles(documentStore => documentStore
    .WithCosmosDb(options =>
    {
        options.ConnectionString = "AccountEndpoint=https://...";
        options.DatabaseName = "chronicles-events";
        options.ContainerName = "events"; // Optional, defaults to "events"
        options.ThroughputProperties = new(400); // Optional
    })
    .WithEventStore(eventStore => eventStore
        .WithCqrs(cqrs => cqrs
            .AddCommandHandler<CreateOrderCommand, OrderState, CreateOrderHandler>())));
```

### Event Store Options

Customize event store behavior:

```csharp
builder.Services.AddChronicles(documentStore => documentStore
    .WithEventStore(eventStore => eventStore
        .Configure<EventStoreOptions>(options =>
        {
            options.StreamOptions = new StreamOptions
            {
                MaxBatchSize = 100,
                DefaultTtl = TimeSpan.FromDays(365)
            };
        })));
```

### CQRS Configuration

Register command handlers and configure CQRS:

```csharp
builder.Services.AddChronicles(documentStore => documentStore
    .WithEventStore(eventStore => eventStore
        .WithCqrs(cqrs => cqrs
            .AddCommandHandler<CreateOrderCommand, OrderState, CreateOrderHandler>()
            .AddCommandHandler<ShipOrderCommand, OrderState, ShipOrderHandler>()
            // Configure command options
            .ConfigureCommands<CreateOrderCommand>(options =>
            {
                options.Consistency = CommandConsistency.Strong;
                options.ConflictBehavior = CommandConflictBehavior.Retry;
            }))));
```

## Usage Examples

### Complete Order Management Example

```csharp
// Events
public record OrderCreated(string OrderId, string CustomerId, DateTime CreatedAt);
public record OrderItemAdded(string OrderId, string ProductId, int Quantity, decimal Price);
public record OrderShipped(string OrderId, DateTime ShippedAt, string TrackingNumber);

// Commands
public record CreateOrderCommand(string OrderId, string CustomerId);
public record AddOrderItemCommand(string OrderId, string ProductId, int Quantity, decimal Price);
public record ShipOrderCommand(string OrderId, string TrackingNumber);

// State
public class OrderState
{
    public string? OrderId { get; set; }
    public string? CustomerId { get; set; }
    public List<OrderItem> Items { get; set; } = new();
    public bool IsShipped { get; set; }
    public bool IsCreated => !string.IsNullOrEmpty(OrderId);
}

// Command Handler
public class OrderCommandHandler : 
    ICommandHandler<CreateOrderCommand, OrderState>,
    ICommandHandler<AddOrderItemCommand, OrderState>,
    ICommandHandler<ShipOrderCommand, OrderState>
{
    public async ValueTask ExecuteAsync(
        ICommandContext<CreateOrderCommand> context,
        OrderState state,
        CancellationToken cancellationToken)
    {
        if (state.IsCreated)
            throw new InvalidOperationException("Order already exists");

        await context.EmitAsync(new OrderCreated(
            context.Command.OrderId,
            context.Command.CustomerId,
            DateTime.UtcNow));
    }

    public async ValueTask ExecuteAsync(
        ICommandContext<AddOrderItemCommand> context,
        OrderState state,
        CancellationToken cancellationToken)
    {
        if (!state.IsCreated)
            throw new InvalidOperationException("Order not found");

        if (state.IsShipped)
            throw new InvalidOperationException("Cannot modify shipped order");

        await context.EmitAsync(new OrderItemAdded(
            context.Command.OrderId,
            context.Command.ProductId,
            context.Command.Quantity,
            context.Command.Price));
    }

    public async ValueTask ExecuteAsync(
        ICommandContext<ShipOrderCommand> context,
        OrderState state,
        CancellationToken cancellationToken)
    {
        if (!state.IsCreated)
            throw new InvalidOperationException("Order not found");

        if (state.IsShipped)
            throw new InvalidOperationException("Order already shipped");

        if (!state.Items.Any())
            throw new InvalidOperationException("Cannot ship empty order");

        await context.EmitAsync(new OrderShipped(
            context.Command.OrderId,
            DateTime.UtcNow,
            context.Command.TrackingNumber));
    }

    // State projection methods
    public void ConsumeEvent(StreamEvent evt, CreateOrderCommand command, IStateContext state)
    {
        if (evt.Data is OrderCreated created)
        {
            state.Set<OrderState>(s => s with 
            { 
                OrderId = created.OrderId,
                CustomerId = created.CustomerId 
            });
        }
    }

    // Similar ConsumeEvent methods for other commands...
}
```

### Event Processing and Projections

```csharp
public class OrderAnalyticsProcessor : IEventProcessor
{
    private readonly IDocumentPublisher _documentPublisher;

    public OrderAnalyticsProcessor(IDocumentPublisher documentPublisher)
    {
        _documentPublisher = documentPublisher;
    }

    public async Task ProcessAsync(StreamEvent streamEvent, CancellationToken cancellationToken)
    {
        switch (streamEvent.Data)
        {
            case OrderCreated created:
                await UpdateDailyStats(created.CreatedAt.Date, stats => stats.OrdersCreated++);
                break;

            case OrderShipped shipped:
                await UpdateDailyStats(shipped.ShippedAt.Date, stats => stats.OrdersShipped++);
                break;
        }
    }

    private async Task UpdateDailyStats(DateTime date, Action<DailyStats> update)
    {
        var documentId = $"daily-stats-{date:yyyy-MM-dd}";
        var stats = await _documentPublisher.GetAsync<DailyStats>(documentId) 
                   ?? new DailyStats { Date = date };
        
        update(stats);
        
        await _documentPublisher.PublishAsync(documentId, stats);
    }
}
```

## Sample Applications

The repository includes comprehensive sample applications demonstrating real-world usage:

- **[Restaurant API](sample/Chronicles.RestaurantApi/)**: Restaurant management with menu items and availability
- **[Order API](sample/Chronicles.OrderApi/)**: Order processing and lifecycle management  
- **[Courier API](sample/Chronicles.CourierApi/)**: Delivery tracking and courier management
- **[App Host](sample/Chronicles.AppHost/)**: .NET Aspire host for running all services

The samples are based on event-modeled systems for restaurant ordering:
- [Courier System Model](https://github.com/fraktalio/courier-demo/blob/main/.assets/event-model.jpg)
- [Order System Model](https://github.com/fraktalio/order-demo/blob/main/.assets/event-model.jpg)
- [Restaurant System Model](https://github.com/fraktalio/restaurant-demo/blob/main/.assets/event-model.jpg)

### Running the Samples

```bash
cd sample
dotnet run --project Chronicles.AppHost
```

This starts all services using .NET Aspire for easy development and testing.

## Testing

Chronicles provides comprehensive testing utilities for event sourcing scenarios:

```csharp
[Test]
public async Task Should_Create_Order_When_Valid_Command_Executed()
{
    // Arrange
    var streamId = new StreamId("orders", "test-order");
    var command = new CreateOrderCommand("test-order", "customer-123");

    // Act
    var result = await commandProcessor.ExecuteAsync(command);

    // Assert
    result.IsSuccess.Should().BeTrue();
    
    var events = await eventReader.ReadAsync(streamId).ToListAsync();
    events.Should().ContainSingle()
          .Which.Data.Should().BeOfType<OrderCreated>()
          .Which.OrderId.Should().Be("test-order");
}
```

### Test Utilities

Chronicles includes testing utilities for:
- In-memory event stores for unit testing
- Test fixtures for integration testing
- Event assertion helpers
- State verification utilities

## Contributing

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) for details on:

- How to submit bug reports and feature requests
- Development workflow and coding standards  
- Pull request process
- Code of conduct

### Quick Contribution Guide

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/your-feature`
3. Make your changes following our coding conventions
4. Add tests for your changes
5. Ensure all tests pass: `dotnet test`
6. Submit a pull request

Please read our [Code of Conduct](CODE-OF-CONDUCT.md) before contributing.

## License

This project is licensed under the [MIT License](LICENSE) - see the LICENSE file for details.

## Support

- 📖 [Documentation](docs/)
- 💬 [Discussions](https://github.com/chronicles-net/chronicles/discussions)
- 🐛 [Issues](https://github.com/chronicles-net/chronicles/issues)
- 📧 Contact the maintainers

---

Made with ❤️ by the Chronicles team