# Event Evolution

## Overview

Events in an event-sourced system accumulate over months and years. Business requirements evolve, domain language matures, and event schemas must adapt. Unlike traditional databases, event stores are append-only—you cannot "ALTER TABLE" your way out of schema changes.

Chronicles handles this gracefully: streams **never throw**. When an event cannot be deserialized or is unrecognized, it is wrapped in a `FaultedEvent` or `UnknownEvent` respectively, allowing projections to skip gracefully, log warnings, and continue processing. This guide covers the patterns and APIs for managing event evolution in Chronicles.

For general event sourcing theory, see [Event Sourcing: Building Event-Driven Systems](https://martinfowler.com/eaaDev/EventSourcing.html) (Martin Fowler) and [Versioning in an Event Sourced System](https://www.eventstore.com/blog/versioning-in-event-sourced-systems) (Event Store).

---

## Patterns

### Pattern 1: Event Rename (Using Aliases)

**Scenario:** Your ubiquitous language evolves. `OrderPlaced` is renamed to `OrderCreated` to better reflect domain intent.

**Historical problem:** Streams contain thousands of `order-placed` events. New code writes `order-created`. The old and new event names represent the same domain concept.

**Solution:** Register the new event with aliases for the old names.

```csharp
// Register with alias — one line, no custom converter
builder.AddEvent<OrderCreated>("order-created", aliases: "order-placed");
```

The `aliases` parameter accepts multiple legacy names:

```csharp
// Event renamed twice over system lifetime
builder.AddEvent<OrderCreated>("order-created", 
    aliases: ["order-placed", "OrderPlacedEvent"]);
```

**How it works:**
- **Primary name** (`"order-created"`) is used when writing new events
- **Aliases** (`"order-placed"`, etc.) are recognized during deserialization of historical events
- Aliases are **read-only** — never written to the event stream
- Each alias maps to the same `OrderCreated` type and converter

**When to use:**
- Ubiquitous language refinements (naming convention changes, domain terminology updates)
- Simple renames with no schema changes
- Deprecating old event names while maintaining backward compatibility

**Complete example:**

```csharp
public record OrderCreated(
    string OrderId,
    decimal Amount,
    DateTimeOffset CreatedAt);

// Service configuration
services.AddChronicles(chronicles => chronicles
    .AddEventStore("orders", store => store
        .AddEvent<OrderCreated>("order-created", aliases: "order-placed")
        .AddEvent<OrderShipped>("order-shipped")
        .AddEvent<OrderCancelled>("order-cancelled")));

// Projection handles both old and new events identically
public class OrderProjection : IStateProjection<OrderState>
{
    public OrderState Apply(OrderState state, object @event)
        => @event switch
        {
            // Handles both "order-placed" (aliased) and "order-created" (primary)
            OrderCreated e => state with { Status = "Created", Amount = e.Amount },
            OrderShipped e => state with { Status = "Shipped" },
            OrderCancelled e => state with { Status = "Cancelled" },
            _ => state
        };
}
```

---

### Pattern 2: Field Addition (Backwards Compatible)

**Scenario:** New feature: multi-currency support. All `PaymentReceived` events now include a `Currency` field, but historical events only have `Amount`.

**Solution:** Use optional fields with default values. JSON deserialization handles missing fields automatically.

```csharp
// V1 (historical)
public record PaymentReceived(decimal Amount);

// V2 (current) — add optional field with default
public record PaymentReceived(decimal Amount, string Currency = "USD");
```

**How it works:**
- When deserialization encounters a JSON object missing the `Currency` field, the default value `"USD"` is applied
- No custom converter needed — this is built into C# record deserialization
- Existing events seamlessly gain the default value

**Register as normal:**

```csharp
builder.AddEvent<PaymentReceived>("payment-received");
```

**When to use:**
- Adding new optional fields to events
- Setting sensible defaults for missing data (currency, timezone, etc.)
- Maintaining schema compatibility when extending events

**Guidelines:**

| Scenario | Approach |
|----------|----------|
| New field, sensible default | `string Currency = "USD"` (required param with default) |
| New field, may be absent | `string? Currency = null` (nullable) |
| New field, no default | Use custom converter (see Pattern 3) |

---

### Pattern 3: Custom Converter (Type Changes)

**Scenario:** Business domain evolves. `Amount` changes from a primitive `decimal` to a value object `Money` (amount + currency).

**Problem:** Old events have `{ "Amount": 99.99 }`. New events need `{ "Amount": { "Value": 99.99, "Currency": "USD" } }`. JSON deserialization alone cannot bridge this gap.

**Solution:** Implement `IEventDataConverter` to transform old event structures into new types.

```csharp
public class PaymentReceivedConverter : IEventDataConverter
{
    public object? Convert(EventConverterContext context)
    {
        // Only handle events with this name
        if (context.Metadata.Name != "payment-received")
            return null;

        // V1 structure: decimal Amount
        if (context.Data.TryGetProperty("Amount", out var amountProp) 
            && amountProp.ValueKind == JsonValueKind.Number)
        {
            var amount = amountProp.GetDecimal();
            var currency = context.Data.TryGetProperty("Currency", out var currencyProp) 
                ? currencyProp.GetString() ?? "USD"
                : "USD";
            
            return new PaymentReceived(new Money(amount, currency));
        }

        // V2 structure: Money object — deserialize normally
        return context.Data.Deserialize<PaymentReceived>(context.Options);
    }
}

// Register the custom converter
builder.AddEvent<PaymentReceived>("payment-received", new PaymentReceivedConverter());
```

**How it works:**
- `IEventDataConverter.Convert()` receives `EventConverterContext` containing the raw JSON data and event metadata
- Return the converted `PaymentReceived` instance
- Return `null` if the converter doesn't recognize the event (enables multi-event converters)
- Exceptions are caught by the framework and wrapped in `FaultedEvent`

**When to use:**
- Type changes (primitive → value object, enum → class, etc.)
- Structural reorganization (flattened fields → nested objects, vice versa)
- Complex data transformations during deserialization

**EventConverterContext members:**
- `JsonElement Data` — the raw event JSON
- `EventMetadata Metadata` — event name, stream ID, version, timestamp
- `JsonSerializerOptions Options` — configured serializer options for consistent deserialization

---

### Pattern 4: Handling Unknown and Faulted Events

**Scenario:** Your system is upgraded to a newer version that writes new event types. Your older version reads the stream and encounters events it doesn't recognize.

**What happens:**
- **UnknownEvent:** Event name not registered in `EventCatalog` — wrapped with the raw JSON
- **FaultedEvent:** Deserialization threw an exception — wrapped with JSON and exception details

**Projections should skip gracefully:**

```csharp
public class ResilientOrderProjection : IStateProjection<OrderState>
{
    private readonly ILogger<ResilientOrderProjection> logger;

    public OrderState Apply(OrderState state, object @event)
        => @event switch
        {
            OrderCreated e => state with { Status = "Created" },
            OrderShipped e => state with { Status = "Shipped" },
            
            // Handle evolution gracefully
            UnknownEvent u => HandleUnknownEvent(state, u),
            FaultedEvent f => HandleFaultedEvent(state, f),
            
            _ => state
        };

    private OrderState HandleUnknownEvent(OrderState state, UnknownEvent u)
    {
        logger.LogWarning(
            "Skipped unknown event. Upgrade system to handle new event types. JSON: {Json}",
            u.Json);
        return state;  // No state change
    }

    private OrderState HandleFaultedEvent(OrderState state, FaultedEvent f)
    {
        logger.LogError(f.Exception,
            "Deserialization failed. Check event schema compatibility. JSON: {Json}",
            f.Json);
        return state;  // No state change
    }
}
```

**Document projections work identically:**

```csharp
public class OrderDocumentProjection : IDocumentProjection<OrderDocument>
{
    private readonly ILogger<OrderDocumentProjection> logger;

    public OrderDocument Apply(OrderDocument doc, StreamEvent @event)
        => @event.Data switch
        {
            OrderCreated e => ApplyOrderCreated(doc, e),
            OrderShipped e => ApplyOrderShipped(doc, e),
            
            UnknownEvent _ => HandleUnknown(doc, @event),
            FaultedEvent f => HandleFaulted(doc, f, @event),
            
            _ => doc
        };

    private OrderDocument HandleUnknown(OrderDocument doc, StreamEvent @event)
    {
        logger.LogWarning(
            "Unknown event '{Name}' in stream {StreamId}. " +
            "System version is older than event producer. Consider upgrade.",
            @event.Metadata.Name, @event.Metadata.StreamId);
        return doc;
    }

    private OrderDocument HandleFaulted(OrderDocument doc, FaultedEvent f, StreamEvent @event)
    {
        logger.LogError(f.Exception,
            "Failed to deserialize '{Name}' in stream {StreamId}",
            @event.Metadata.Name, @event.Metadata.StreamId);
        return doc;
    }
}
```

**When to use:**
- Projections reading from streams in multi-system environments
- Gradual deployments where systems have different versions
- Forward-compatibility (your system reads events from a newer producer)

**Key insight:** Skipping unknown/faulted events is the right behavior for read-side projections. Streams are already persisted; you cannot "fix" them by throwing. Log the issue for operational visibility and continue.

---

## Testing

### Testing Custom Converters

Verify that your converter handles both old and new event structures:

```csharp
[Fact]
public void PaymentReceivedConverter_TransformsV1DecimalToV2Money()
{
    // Arrange
    var converter = new PaymentReceivedConverter();
    var oldJson = JsonDocument.Parse(
        """{"Amount": 99.99}""").RootElement;
    
    var metadata = new EventMetadata(
        Name: "payment-received",
        StreamId: StreamId.For<Order>("123"),
        Version: 1,
        Timestamp: DateTimeOffset.UtcNow);
    
    var context = new EventConverterContext(
        Data: oldJson,
        Metadata: metadata,
        Options: new JsonSerializerOptions());

    // Act
    var result = converter.Convert(context);

    // Assert
    Assert.NotNull(result);
    var payment = Assert.IsType<PaymentReceived>(result);
    Assert.Equal(99.99m, payment.Amount.Value);
    Assert.Equal("USD", payment.Amount.Currency);
}

[Fact]
public void PaymentReceivedConverter_DeserializesV2NormallyWhenStructurePresent()
{
    // Arrange
    var converter = new PaymentReceivedConverter();
    var newJson = JsonDocument.Parse(
        """{"Amount": {"Value": 99.99, "Currency": "EUR"}}""").RootElement;
    
    var context = new EventConverterContext(
        Data: newJson,
        Metadata: new EventMetadata(
            Name: "payment-received",
            StreamId: StreamId.For<Order>("123"),
            Version: 5,
            Timestamp: DateTimeOffset.UtcNow),
        Options: new JsonSerializerOptions());

    // Act
    var result = converter.Convert(context);

    // Assert
    Assert.NotNull(result);
    var payment = Assert.IsType<PaymentReceived>(result);
    Assert.Equal(99.99m, payment.Amount.Value);
    Assert.Equal("EUR", payment.Amount.Currency);
}

[Fact]
public void PaymentReceivedConverter_ReturnsNullForUnrelatedEvents()
{
    // Arrange
    var converter = new PaymentReceivedConverter();
    var context = new EventConverterContext(
        Data: JsonDocument.Parse("""{"Value": 50}""").RootElement,
        Metadata: new EventMetadata(
            Name: "order-shipped",  // Not payment-received
            StreamId: StreamId.For<Order>("123"),
            Version: 2,
            Timestamp: DateTimeOffset.UtcNow),
        Options: new JsonSerializerOptions());

    // Act
    var result = converter.Convert(context);

    // Assert — null signals "this converter doesn't handle this event"
    Assert.Null(result);
}
```

### Testing Mixed-Version Streams

Verify that a projection correctly handles a stream containing both old (aliased) and new event names:

```csharp
[Fact]
public async Task OrderProjection_HandlesMixedVersionStream_WithAliasedAndNewEventNames()
{
    // Arrange
    var streamId = StreamId.For<Order>("ord-123");
    
    // Simulate a real stream with historical and new events
    var events = new[]
    {
        // Historical event with old name (would exist in Cosmos DB)
        CreateStreamEvent(1, "order-placed", new { OrderId = "ord-123", Amount = 100m }),
        // New event with new name
        CreateStreamEvent(2, "order-created", new { OrderId = "ord-123", Amount = 200m }),
        // More historical (old name)
        CreateStreamEvent(3, "order-placed", new { OrderId = "ord-123", Amount = 50m })
    };

    var projection = new OrderProjection();
    var state = OrderState.Empty;

    // Act
    foreach (var @event in events)
    {
        state = projection.Apply(state, @event);
    }

    // Assert: Both old and new events were processed
    Assert.Equal("Created", state.Status);
    Assert.Equal(350m, state.TotalAmount);
}

private StreamEvent CreateStreamEvent(long version, string name, object data)
{
    var json = JsonSerializer.Serialize(data);
    var metadata = new EventMetadata(
        Name: name,
        StreamId: StreamId.For<Order>("ord-123"),
        Version: version,
        Timestamp: DateTimeOffset.UtcNow);
    
    return new StreamEvent(
        Data: data,
        Metadata: metadata);
}
```

### Testing Unknown and Faulted Events

Verify that projections gracefully skip unrecognized events:

```csharp
[Fact]
public void Projection_SkipsUnknownEvents_WithoutThrowing()
{
    // Arrange
    var projection = new ResilientOrderProjection(Mock.Of<ILogger<ResilientOrderProjection>>());
    var state = OrderState.Empty;
    var unknownEvent = new UnknownEvent("""{"mysterious": "data"}""");

    // Act & Assert — should not throw
    var result = projection.Apply(state, unknownEvent);
    
    Assert.Equal(state, result);  // State unchanged
}

[Fact]
public void Projection_SkipsFaultedEvents_WithoutThrowing()
{
    // Arrange
    var logger = new Mock<ILogger<ResilientOrderProjection>>();
    var projection = new ResilientOrderProjection(logger.Object);
    var state = OrderState.Empty;
    var faultedException = new JsonException("Invalid JSON");
    var faultedEvent = new FaultedEvent("""{"corrupt": invalid}""", faultedException);

    // Act & Assert — should not throw
    var result = projection.Apply(state, faultedEvent);
    
    Assert.Equal(state, result);  // State unchanged
    
    // Verify logging occurred
    logger.Verify(
        x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
}
```

---

## FAQ

### Q: Can I delete old events?
**A:** No. Event stores are append-only. Deleting events breaks the event sourcing contract. If you must remove sensitive data (PII, payment details), implement a separate anonymization process or use encryption with key rotation. The event itself remains; only its content is obscured.

### Q: Can I change an event's JSON structure in Cosmos DB directly?
**A:** No. Do not manually edit events in the database. Use custom converters (`IEventDataConverter`) to handle structural changes during deserialization. Manual edits bypass the conversion pipeline and corrupt the historical record.

### Q: How do aliases work?
**A:** Aliases are **read-only mappings**. When you register:

```csharp
builder.AddEvent<OrderCreated>("order-created", aliases: "order-placed");
```

The `EventCatalog` internally creates two entries:
- `"order-created"` → `OrderCreated` converter (primary, used for writing)
- `"order-placed"` → same `OrderCreated` converter (alias, used for reading)

New events are always written with the primary name `"order-created"`. Old events with the alias name `"order-placed"` are recognized and deserialized identically. To downstream projections, both are `OrderCreated` instances.

### Q: What happens to events from a newer system version?
**A:** If a newer system writes an event type that your system doesn't recognize, you get an `UnknownEvent` wrapper. Projections should skip it gracefully (see Pattern 4). This enables blue-green deployments and gradual system upgrades.

### Q: When should I use a custom converter vs. an alias?
**A:** 
- **Alias:** Simple renames (event name only, no schema change)
- **Custom converter:** Type changes, structural reorganization, complex transformations

Example:
- Rename `OrderPlaced` → `OrderCreated` → use alias
- Change `Amount: decimal` → `Amount: Money` → use custom converter

### Q: Can I have multiple converters for the same event?
**A:** No. Each event name maps to one converter. If you need multiple transformations, compose them in a single converter (check multiple conditions, apply different transforms, return the result).

### Q: What if a custom converter throws an exception?
**A:** The exception is caught by the Chronicles framework and wrapped in `FaultedEvent(json, exception)`. The stream read continues; the faulted event is available to projections via pattern matching. Log the error for operational visibility.

### Q: Are aliases supported across multiple `AddEvent` calls?
**A:** No. Aliases are defined per registration:

```csharp
// This registers one alias for OrderCreated
builder.AddEvent<OrderCreated>("order-created", aliases: "order-placed");

// This registers OrderPlaced as a separate event with no aliases
builder.AddEvent<OrderPlaced>("order-placed-v2");
```

If you attempt to register the same name or alias twice, Chronicles throws `InvalidOperationException` at configuration time.

---

## Advanced Topics

### Forward Compatibility

When your system reads events from a **newer version**, unknown event types appear as `UnknownEvent`. This is the correct behavior for blue-green deployments:

1. **Blue (old) system** reads stream
2. **Green (new) system** writes new event types
3. **Blue system** encounters `UnknownEvent` → skips gracefully
4. Operator upgrades Blue to Green → new events are now understood

No manual intervention needed. Streams remain valid.

### Multi-System Environments

In microservices architectures, different services may use different event schemas for the same domain concept. Use custom converters to normalize:

```csharp
public class NormalizedPaymentConverter : IEventDataConverter
{
    public object? Convert(EventConverterContext context)
    {
        return context.Metadata.Name switch
        {
            "payment-received" => context.Data.Deserialize<PaymentReceived>(context.Options),
            "payment-processed" => ConvertPaymentProcessed(context),
            "charge-completed" => ConvertChargeCompleted(context),
            _ => null
        };
    }

    private PaymentReceived ConvertPaymentProcessed(EventConverterContext context) { ... }
    private PaymentReceived ConvertChargeCompleted(EventConverterContext context) { ... }
}
```

### Future Enhancements

The following features are planned for future releases based on user feedback:

- **Fluent upcasting API:** Chainable transformations without implementing `IEventDataConverter`
- **Lambda converters:** Inline conversion functions instead of full class implementations
- **Schema versioning:** Explicit version tracking on events for multi-version transformations
- **Event deprecation:** Mark events as deprecated with compile-time/runtime warnings

---

## Summary

Event evolution in Chronicles is **safe by design**:
- Streams never throw
- Unknown and faulted events are wrapped, never propagated
- Projections pattern-match on event types and skip gracefully
- Three APIs cover common scenarios: **aliases for renames**, **optional fields for additions**, **custom converters for type changes**

The result: your event-sourced system remains operational and resilient as business requirements evolve.
