# Dependency Injection

Chronicles uses .NET dependency injection to configure event stores, document stores, command handlers, and projections.

## Basic Setup

Use `AddChronicles` to register Chronicles services:

```csharp
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddChronicles(store =>
{
    store.Configure(options =>
    {
        options.UseConnectionString(builder.Configuration["Cosmos:ConnectionString"]!);
        options.UseDatabase(builder.Configuration["Cosmos:DatabaseName"]!);
    });
});

var app = builder.Build();
app.Run();
```

This registers:

- `IDocumentReader<T>`
- `IDocumentWriter<T>`
- `IEventStreamReader`
- `IEventStreamWriter`
- Supporting infrastructure

## Event Store Configuration

Add the event store with event registrations:

```csharp
builder.Services.AddChronicles(store =>
{
    store.Configure(options =>
    {
        options.UseConnectionString("your-connection-string");
        options.UseDatabase("your-database");
    });

    store.WithEventStore(eventStore =>
    {
        eventStore.AddEvent<OrderPlaced>("order-placed:v1");
        eventStore.AddEvent<OrderShipped>("order-shipped:v1");
        eventStore.AddEvent<OrderCancelled>("order-cancelled:v1");
        eventStore.AddEvent<OrderRefunded>("order-refunded:v1");
    });
});
```

### Event Aliases (Event Evolution)

Register event aliases to support legacy event names during deserialization:

```csharp
eventStore.AddEvent<OrderCreated>("order-created", 
    aliases: ["order-placed", "OrderPlacedEvent"]);
```

This allows you to rename events over time while maintaining backward compatibility with historical data. The primary name is used for new events; aliases are recognized during deserialization only.

See [Event Evolution](event-evolution.md) for detailed patterns and strategies.

**Event Store Options:**

```csharp
store.WithEventStore(options =>
{
    options.EventStoreContainer = "events";
    options.StreamIndexContainer = "stream-index";
}, eventStore =>
{
    eventStore.AddEvent<OrderPlaced>("order-placed:v1");
});
```

## CQRS Command Handlers

Register command handlers with `WithCqrs`:

```csharp
builder.Services.AddChronicles(store =>
{
    store.Configure(options =>
    {
        options.UseConnectionString("your-connection-string");
        options.UseDatabase("your-database");
    });

    store.WithEventStore(eventStore =>
    {
        eventStore.AddEvent<OrderPlaced>("order-placed:v1");
        eventStore.AddEvent<OrderShipped>("order-shipped:v1");
        eventStore.AddEvent<OrderCancelled>("order-cancelled:v1");

        eventStore.WithCqrs(cqrs =>
        {
            // Stateless command handler
            cqrs.AddCommand<PlaceOrder, PlaceOrderHandler>();

            // Stateful command handler with state
            cqrs.AddCommand<ShipOrder, ShipOrderHandler, OrderState>();

            // Stateful command handler with options
            cqrs.AddCommand<CancelOrder, CancelOrderHandler>(new CommandOptions
            {
                ConflictBehavior = CommandConflictBehavior.Retry,
                Retry = 5
            });
        });
    });
});
```

This registers:

- `ICommandProcessor<PlaceOrder>`
- `ICommandProcessor<ShipOrder>`
- `ICommandProcessor<CancelOrder>`
- `ICommandProcessorFactory`

**CommandOptions:**

```csharp
var options = new CommandOptions
{
    RequiredState = StreamState.New,               // Require stream to be new
    ConflictBehavior = CommandConflictBehavior.Retry, // Retry on conflict
    Retry = 3,                                     // Retry up to 3 times
    Consistency = CommandConsistency.ReadWrite     // Consistency level
};

cqrs.AddCommand<PlaceOrder, PlaceOrderHandler>(options);
```

## Event Subscriptions

Register event subscriptions to process events from the change feed:

```csharp
store.WithEventStore(eventStore =>
{
    eventStore.AddEvent<OrderPlaced>("order-placed:v1");
    eventStore.AddEvent<OrderShipped>("order-shipped:v1");
    eventStore.AddEvent<OrderCancelled>("order-cancelled:v1");

    eventStore.AddEventSubscription("order-subscription", subscription =>
    {
        subscription.MapAllStreams(processor =>
        {
            processor.AddDocumentProjection<OrderDocument, OrderDocumentProjection>();
            processor.AddDocumentProjection<OrderSummaryDocument, OrderSummaryProjection>();
            processor.AddEventProcessor<OrderEventLogger>();
        });
    });
});
```

**Subscription Options:**

```csharp
eventStore.AddEventSubscription("order-subscription", options =>
{
    options.SubscriptionOptions.StartOptions = SubscriptionStartOptions.FromBeginning;
    options.SubscriptionOptions.BatchSize = 100;
    options.SubscriptionOptions.PollingInterval = TimeSpan.FromSeconds(5);
}, subscription =>
{
    subscription.MapAllStreams(processor =>
    {
        processor.AddDocumentProjection<OrderDocument, OrderDocumentProjection>();
    });
});
```

## Document Options

Configure document containers and initialization:

```csharp
builder.Services.AddChronicles(store =>
{
    store.Configure(options =>
    {
        options.UseConnectionString("your-connection-string");
        options.UseDatabase("your-database");
        options.AddInitialization(init =>
        {
            init.CreateDatabase(ThroughputProperties.CreateManualThroughput(400));
            init.CreateSubscriptionContainer();
        });
    });
});
```

**InitializationOptions:**

- `CreateDatabase(ThroughputProperties?)`: Create the database on startup with optional throughput (default: 400 RU)
- `CreateSubscriptionContainer(ThroughputProperties?)`: Create the subscription container
- `CreateContainer<T>(Action<ContainerProperties>?, ThroughputProperties?)`: Create a container for a document type

## Multiple Document Stores

Configure multiple document stores for different databases or accounts:

```csharp
builder.Services.AddChronicles()
    .AddDocumentStore("primary", store =>
    {
        store.Configure(options =>
        {
            options.UseConnectionString(builder.Configuration["Cosmos:Primary:ConnectionString"]!);
            options.UseDatabase("primary-database");
        });

        store.WithEventStore(eventStore =>
        {
            eventStore.AddEvent<OrderPlaced>("order-placed:v1");

            eventStore.WithCqrs(cqrs =>
            {
                cqrs.AddCommand<PlaceOrder, PlaceOrderHandler>();
            });
        });
    })
    .AddDocumentStore("archive", store =>
    {
        store.Configure(options =>
        {
            options.UseConnectionString(builder.Configuration["Cosmos:Archive:ConnectionString"]!);
            options.UseDatabase("archive-database");
        });
    });
```

Specify the store name when using services:

```csharp
await _reader.ReadAsync<OrderDocument>(
    documentId: "ord-123",
    partitionKey: "ord-123",
    options: null,
    storeName: "archive",
    cancellationToken: cancellationToken);
```

## CqrsBuilder

The `CqrsBuilder` returned by `WithCqrs` provides:

```csharp
eventStore.WithCqrs(cqrs =>
{
    // Register stateless command handler
    cqrs.AddCommand<PlaceOrder, PlaceOrderHandler>();

    // Register stateful command handler with state type
    cqrs.AddCommand<ShipOrder, ShipOrderHandler, OrderState>();

    // Register command handler with options
    cqrs.AddCommand<CancelOrder, CancelOrderHandler>(new CommandOptions
    {
        ConflictBehavior = CommandConflictBehavior.Retry
    });
});
```

Access the underlying `IServiceCollection`:

```csharp
eventStore.WithCqrs(cqrs =>
{
    cqrs.Services.AddSingleton<IOrderValidator, OrderValidator>();
    cqrs.AddCommand<PlaceOrder, PlaceOrderHandler>();
});
```

## Testing with AddFakeChronicles

Use `AddFakeChronicles` as a drop-in replacement for testing:

```csharp
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddFakeChronicles(store =>
{
    store.WithEventStore(eventStore =>
    {
        eventStore.AddEvent<OrderPlaced>("order-placed:v1");
        eventStore.AddEvent<OrderShipped>("order-shipped:v1");

        eventStore.WithCqrs(cqrs =>
        {
            cqrs.AddCommand<PlaceOrder, PlaceOrderHandler>();
        });
    });
});

var provider = services.BuildServiceProvider();

var writer = provider.GetRequiredService<IEventStreamWriter>();
var processor = provider.GetRequiredService<ICommandProcessor<PlaceOrder>>();
```

`AddFakeChronicles`:

- Replaces Cosmos DB with in-memory storage
- Provides the same API surface as `AddChronicles`
- Does not require Cosmos DB connection
- Suitable for unit and integration tests

## Configuration Example

Full configuration with all features:

```csharp
builder.Services.AddChronicles(store =>
{
    store.Configure(options =>
    {
        options.UseConnectionString(builder.Configuration["Cosmos:ConnectionString"]!);
        options.UseDatabase(builder.Configuration["Cosmos:DatabaseName"]!);
        options.AddInitialization(init =>
        {
            init.CreateDatabase(ThroughputProperties.CreateManualThroughput(400));
            init.CreateSubscriptionContainer();
        });
    });

    store.WithEventStore(options =>
    {
        options.EventStoreContainer = "events";
    },
    eventStore =>
    {
        eventStore.AddEvent<OrderPlaced>("order-placed:v1");
        eventStore.AddEvent<OrderShipped>("order-shipped:v1");
        eventStore.AddEvent<OrderCancelled>("order-cancelled:v1");

        eventStore.AddEventSubscription("order-subscription", options =>
        {
            options.SubscriptionOptions.StartOptions = SubscriptionStartOptions.FromNow;
            options.SubscriptionOptions.BatchSize = 100;
        }, subscription =>
        {
            subscription.MapAllStreams(processor =>
            {
                processor.AddDocumentProjection<OrderDocument, OrderDocumentProjection>();
                processor.AddEventProcessor<OrderEventLogger>();
            });
        });

        eventStore.WithCqrs(cqrs =>
        {
            cqrs.AddCommand<PlaceOrder, PlaceOrderHandler>();
            cqrs.AddCommand<ShipOrder, ShipOrderHandler, OrderState>();
            cqrs.AddCommand<CancelOrder, CancelOrderHandler>(new CommandOptions
            {
                ConflictBehavior = CommandConflictBehavior.Retry,
                Retry = 3
            });
        });
    });
});

// Register custom exception handler
builder.Services.AddSingleton<IEventSubscriptionExceptionHandler, CustomExceptionHandler>();
```

## Best Practices

- **Store Cosmos connection strings in configuration** (not source code)
- **Use `AddFakeChronicles` for testing** to avoid Cosmos DB dependencies
- **Register events before command handlers** for proper initialization
- **Use named document stores** for multi-database scenarios
- **Configure subscription options** based on workload characteristics
- **Register custom exception handlers** for production error handling
- **Pre-create containers in production** rather than relying on auto-initialization
