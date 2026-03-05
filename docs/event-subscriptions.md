# Event Subscriptions

Event subscriptions process events from the change feed as they are written to the event store. Use subscriptions to build projections, trigger side effects, or integrate with external systems.

## IEventProcessor

Implement `IEventProcessor` to process events from the change feed:

```csharp
using Chronicles.EventStore;

public class OrderEventLogger : IEventProcessor
{
    private readonly ILogger<OrderEventLogger> _logger;

    public OrderEventLogger(ILogger<OrderEventLogger> logger)
    {
        _logger = logger;
    }

    public ValueTask ConsumeAsync(
        StreamEvent evt,
        IStateContext state,
        bool hasMore,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Event received: {EventType} from stream {StreamId} at version {Version}",
            evt.Metadata.Name,
            evt.Metadata.StreamId,
            evt.Metadata.Version);

        switch (evt.Data)
        {
            case OrderPlaced placed:
                _logger.LogInformation(
                    "Order placed: {OrderId} for customer {CustomerId}, total {TotalAmount}",
                    placed.OrderId,
                    placed.CustomerId,
                    placed.TotalAmount);
                break;

            case OrderShipped shipped:
                _logger.LogInformation(
                    "Order shipped: {OrderId} with tracking {TrackingNumber}",
                    shipped.OrderId,
                    shipped.TrackingNumber);
                break;

            case OrderCancelled cancelled:
                _logger.LogInformation(
                    "Order cancelled: {OrderId}, reason: {Reason}",
                    cancelled.OrderId,
                    cancelled.Reason);
                break;
        }

        return ValueTask.CompletedTask;
    }
}
```

**Parameters:**

- `evt`: The `StreamEvent` being processed
- `state`: An `IStateContext` for tracking state across events
- `hasMore`: `true` if more events are coming in this batch, `false` for the last event
- `cancellationToken`: Cancellation token

## Registering Event Processors

Register your event processor in the event subscription:

```csharp
builder.Services.AddChronicles(store =>
{
    store.WithEventStore(eventStore =>
    {
        eventStore.AddEvent<OrderPlaced>("order-placed:v1");
        eventStore.AddEvent<OrderShipped>("order-shipped:v1");
        eventStore.AddEvent<OrderCancelled>("order-cancelled:v1");

        eventStore.AddEventSubscription("order-events", subscription =>
        {
            subscription.MapAllStreams(processor =>
            {
                processor.AddEventProcessor<OrderEventLogger>();
            });
        });
    });
});
```

## IStateContext

`IStateContext` allows processors to maintain state across events:

```csharp
public class OrderStatisticsProcessor : IEventProcessor
{
    public ValueTask ConsumeAsync(
        StreamEvent evt,
        IStateContext state,
        bool hasMore,
        CancellationToken cancellationToken)
    {
        var totalOrders = state.GetState<int>("totalOrders");
        var totalRevenue = state.GetState<decimal>("totalRevenue");

        if (evt.Data is OrderPlaced placed)
        {
            state.SetState("totalOrders", totalOrders + 1);
            state.SetState("totalRevenue", totalRevenue + placed.TotalAmount);
        }

        // Flush stats when batch is complete
        if (!hasMore)
        {
            Console.WriteLine($"Processed {totalOrders} orders with total revenue {totalRevenue}");
        }

        return ValueTask.CompletedTask;
    }
}
```

## Document Projections as Event Processors

Document projections are automatically registered as event processors when you use `AddDocumentProjection`:

```csharp
eventStore.AddEventSubscription("order-projection", subscription =>
{
    subscription.MapAllStreams(processor =>
    {
        processor.AddDocumentProjection<OrderDocument, OrderDocumentProjection>();
    });
});
```

This is equivalent to:

```csharp
eventStore.AddEventSubscription("order-projection", subscription =>
{
    subscription.MapAllStreams(processor =>
    {
        processor.AddEventProcessor<DocumentProjectionProcessor<OrderDocumentProjection, OrderDocument>>();
    });
});
```

## Multiple Event Processors

Register multiple processors to handle events:

```csharp
eventStore.AddEventSubscription("order-processing", subscription =>
{
    subscription.MapAllStreams(processor =>
    {
        processor.AddDocumentProjection<OrderDocument, OrderDocumentProjection>();
        processor.AddDocumentProjection<OrderSummaryDocument, OrderSummaryProjection>();
        processor.AddEventProcessor<OrderEventLogger>();
        processor.AddEventProcessor<OrderNotificationSender>();
    });
});
```

All processors run sequentially for each event batch.

## Exception Handling

Implement `IEventSubscriptionExceptionHandler` for custom error handling:

```csharp
using Chronicles.EventStore;

public class CustomExceptionHandler : IEventSubscriptionExceptionHandler
{
    private readonly ILogger<CustomExceptionHandler> _logger;

    public CustomExceptionHandler(ILogger<CustomExceptionHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask HandleAsync(
        Exception exception,
        StreamEvent? streamEvent,
        CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception,
            "Error processing event {EventType} from stream {StreamId}",
            streamEvent?.EventType,
            streamEvent?.StreamId);

        // Log to external monitoring system, send alerts, etc.

        return ValueTask.CompletedTask;
    }
}
```

Register your exception handler:

```csharp
builder.Services.AddSingleton<IEventSubscriptionExceptionHandler, CustomExceptionHandler>();
```

If no custom handler is registered, Chronicles uses `DefaultEventSubscriptionExceptionHandler`, which logs errors to `ILogger`.

## Subscription Options

Configure subscription behavior:

```csharp
eventStore.AddEventSubscription("order-projection", options =>
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

**SubscriptionStartOptions:**

- `FromBeginning`: Process all events from the start of the change feed
- `FromNow`: Process only new events from now forward

**Options:**

- `StartOptions`: Where to start processing (`FromBeginning` or `FromNow`)
- `BatchSize`: Maximum events per batch (default: 100)
- `PollingInterval`: How often to poll for new events (default: 1 second)
- `ForceSingleInstance`: Whether only a single instance of the subscription is allowed

## Real-World Example: Email Notifications

```csharp
using Chronicles.EventStore;

public class OrderEmailNotificationProcessor : IEventProcessor
{
    private readonly IEmailService _emailService;
    private readonly ILogger<OrderEmailNotificationProcessor> _logger;

    public OrderEmailNotificationProcessor(
        IEmailService emailService,
        ILogger<OrderEmailNotificationProcessor> logger)
    {
        _emailService = emailService;
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
            case OrderPlaced placed:
                await _emailService.SendAsync(
                    to: placed.CustomerId,
                    subject: "Order Confirmed",
                    body: $"Your order {placed.OrderId} for ${placed.TotalAmount} has been confirmed.",
                    cancellationToken);

                _logger.LogInformation(
                    "Sent order confirmation email for order {OrderId}",
                    placed.OrderId);
                break;

            case OrderShipped shipped:
                await _emailService.SendAsync(
                    to: shipped.OrderId,
                    subject: "Order Shipped",
                    body: $"Your order {shipped.OrderId} has shipped. Tracking: {shipped.TrackingNumber}",
                    cancellationToken);

                _logger.LogInformation(
                    "Sent shipping notification email for order {OrderId}",
                    shipped.OrderId);
                break;
        }
    }
}
```

Register:

```csharp
eventStore.AddEventSubscription("order-notifications", subscription =>
{
    subscription.MapAllStreams(processor =>
    {
        processor.AddEventProcessor<OrderEmailNotificationProcessor>();
    });
});
```

## Best Practices

- **Keep processors idempotent** — they may process the same event multiple times
- **Use `IStateContext` for batch-level state** — it is reset between batches
- **Handle all event types gracefully** — use pattern matching with default case
- **Implement `IEventSubscriptionExceptionHandler`** for production error handling
- **Use `hasMore` flag** to detect batch boundaries for flushing operations
- **Configure `BatchSize`** based on processing time — smaller batches for heavy work
- **Start from `FromNow` for new processors** to avoid reprocessing historical events
- **Use separate subscriptions** for different processing concerns (projections, notifications, analytics)
