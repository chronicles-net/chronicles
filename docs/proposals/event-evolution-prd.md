# Event Evolution — Product Requirements Document

**Author:** Duncan Idaho (ES/CQRS Expert)  
**Status:** Draft — Pending Maintainer Review  
**Date:** 2026-03-05  
**Target:** Chronicles v1.0

## Summary

Event evolution enables systems to handle schema changes in event-sourced architectures without data migration. This PRD defines the v1.0 scope: multi-name registration for event renames, critical test infrastructure fixes, and comprehensive documentation. Advanced features (fluent upcasting, lambda converters, deprecation support) are explicitly deferred.

---

## 1. Problem Statement

Event-sourced systems accumulate events over years. Business requirements change, domains evolve, and event schemas must adapt. Unlike traditional databases, event stores are append-only—you cannot "ALTER TABLE" your way out of schema changes.

**Common evolution scenarios:**
- **Rename**: `OrderPlaced` → `OrderCreated` (ubiquitous language refinement)
- **Field addition**: New `Currency` field on `PaymentReceived`
- **Type change**: `Amount` from `decimal` to `Money` value object
- **Deprecation**: `LegacyUserRegistered` replaced by `UserRegistered`

**What's awkward today:**
1. **Event renames require custom converters** — even simple name changes need `IEventDataConverter` implementations
2. **Test coverage gaps** — edge cases (malformed JSON, null data, mixed-version streams) lack verification
3. **No documentation** — developers must reverse-engineer the pipeline from source code

---

## 2. Current Architecture

### Deserialization Pipeline

```
┌─────────────────┐    ┌─────────────────┐    ┌──────────────┐    ┌────────────────────┐
│ Cosmos DB JSON  │───▶│ StreamEvent-    │───▶│ EventCatalog │───▶│ IEventDataConverter│
│                 │    │ JsonConverter   │    │ (lookup)     │    │ (Convert)          │
└─────────────────┘    └─────────────────┘    └──────────────┘    └────────────────────┘
                                                     │                      │
                                                     ▼                      ▼
                                              UnknownEvent            FaultedEvent
                                              (no converter)          (exception)
```

### Key Types

| Type | Purpose |
|------|---------|
| `StreamEvent` | Tuple of `(object Data, EventMetadata Metadata)` |
| `FaultedEvent` | Wraps failed deserialization: `(string Json, Exception? Exception)` |
| `UnknownEvent` | Wraps unregistered event names: `(string Json)` |
| `IEventDataConverter` | Contract: `object? Convert(EventConverterContext context)` |
| `EventConverterContext` | Conversion input: `(JsonElement Data, EventMetadata Metadata, JsonSerializerOptions Options)` |
| `EventCatalog` | Dictionary-based name→converter lookup |

### Conversion Flow (StreamEventConverter.cs)

```csharp
// Simplified from src/Chronicles/EventStore/Internal/StreamEventConverter.cs
public StreamEvent Convert(EventConverterContext context)
{
    try
    {
        var data = eventCatalog.GetConverter(context.Metadata.Name)?.Convert(context)
            ?? new UnknownEvent(context.Data.GetRawText());
        return new(data, context.Metadata);
    }
    catch (Exception ex)
    {
        return new(new FaultedEvent(context.Data.GetRawText(), ex), context.Metadata);
    }
}
```

**Critical behavior:** Stream reads **never throw**. Unknown events and deserialization failures are wrapped, not propagated.

### Current Registration API

```csharp
// Basic registration — default EventDataConverter
builder.AddEvent<OrderPlaced>("order-placed");

// Custom converter — full control
builder.AddEvent<OrderPlaced>("order-placed", new OrderPlacedConverter());

// Replace entire catalog — nuclear option
builder.AddEventCatalog<MyCustomCatalog>();
```

---

## 3. Evolution Scenarios

### Scenario 1: Event Rename

**Business context:** Team adopts "Created" suffix convention for domain events.

| Before | After |
|--------|-------|
| Event name: `order-placed` | Event name: `order-created` |
| Type: `OrderPlaced` | Type: `OrderCreated` |

**Historical streams contain:** `order-placed` events  
**New events written as:** `order-created`

### Scenario 2: Field Addition (Backwards Compatible)

**Business context:** Multi-currency support added.

```csharp
// V1 (historical)
public record PaymentReceived(decimal Amount);

// V2 (current)
public record PaymentReceived(decimal Amount, string Currency = "USD");
```

**Requirement:** V1 events deserialize with default `Currency = "USD"`.

### Scenario 3: Type Change (Breaking)

**Business context:** Replace primitive with value object.

```csharp
// V1
public record PaymentReceived(decimal Amount);

// V2
public record PaymentReceived(Money Amount);
```

**Requirement:** Custom converter transforms `decimal` → `Money`.

### Scenario 4: Handling Unknown/Faulted Events

**Business context:** Projection encounters events from newer system version.

**Requirement:** Projection skips gracefully, logs warning, continues processing.

---

## 4. Gap Analysis

| Scenario | Today | Friction |
|----------|-------|----------|
| Event rename | Custom `IEventDataConverter` required | 30+ lines of boilerplate per rename |
| Field addition | Works via JSON defaults | ✅ No friction |
| Type change | Custom converter required | Appropriate complexity |
| Unknown events | `UnknownEvent` wrapper | ✅ Works well |
| Faulted events | `FaultedEvent` wrapper | ✅ Works well |
| **Testing edge cases** | **Gaps exist** | **Critical fixes needed** |

### Test Coverage Gaps (Critical)

1. **Converter returning null** — undocumented, no test verifies `UnknownEvent` result
2. **Malformed JSON** — untested path to `FaultedEvent`
3. **Null data element** — boundary condition untested
4. **Mixed-version streams** — integration test missing

---

## 5. Proposed Changes

### 5a. Multi-Name Registration API

**Goal:** Support event renames without custom converters.

#### API Design

```csharp
/// <summary>
/// Adds an event with optional aliases for backwards compatibility.
/// </summary>
/// <typeparam name="TEvent">Type of event.</typeparam>
/// <param name="name">Primary (canonical) name for new events.</param>
/// <param name="aliases">Legacy names to recognize during deserialization.</param>
/// <returns>The <see cref="EventStoreBuilder"/> for further configurations.</returns>
public EventStoreBuilder AddEvent<TEvent>(
    string name,
    params string[] aliases)
    where TEvent : class
```

#### Usage Examples

**BEFORE — Event Rename (Today)**

```csharp
// Boilerplate converter for simple rename
public class OrderCreatedConverter : IEventDataConverter
{
    private static readonly string[] SupportedNames = ["order-placed", "order-created"];
    
    public object? Convert(EventConverterContext context)
    {
        if (!SupportedNames.Contains(context.Metadata.Name))
            return null;
        
        return context.Data.Deserialize<OrderCreated>(context.Options);
    }
}

// Registration
builder.AddEvent<OrderCreated>("order-created", new OrderCreatedConverter());
```

**AFTER — Event Rename (Proposed)**

```csharp
// One line, no custom converter
builder.AddEvent<OrderCreated>("order-created", aliases: "order-placed");
```

**Multiple Aliases**

```csharp
// Event renamed twice over system lifetime
builder.AddEvent<OrderCreated>("order-created", 
    aliases: ["order-placed", "OrderPlacedEvent"]);
```

#### Implementation Notes

1. **Primary name** (`name`) is used for **writing** new events
2. **Aliases** are **read-only** — recognized during deserialization, never written
3. **Conflict detection** — throw `InvalidOperationException` if alias conflicts with another event's primary name or alias
4. **EventCatalog changes** — `names` dictionary gains entries for each alias pointing to same converter

#### Conflict Detection Example

```csharp
// This should throw InvalidOperationException
builder.AddEvent<OrderCreated>("order-created", aliases: "order-placed");
builder.AddEvent<OrderPlaced>("order-placed"); // CONFLICT: "order-placed" already registered
```

### 5b. Test Infrastructure

**Goal:** Verify all edge cases in the deserialization pipeline.

#### Required Tests

| Test Case | Verifies |
|-----------|----------|
| `Converter_ReturnsNull_ProducesUnknownEvent` | Null return → `UnknownEvent` wrapping |
| `MalformedJson_ProducesFaultedEvent` | Parse failures → `FaultedEvent` wrapping |
| `NullDataElement_ProducesFaultedEvent` | Boundary condition handling |
| `MixedVersionStream_DeserializesAll` | Aliases work in real stream |
| `AliasConflict_ThrowsInvalidOperationException` | Registration validation |
| `DuplicatePrimaryName_ThrowsInvalidOperationException` | Registration validation |

#### Test Helpers (Proposed)

```csharp
namespace Chronicles.Testing;

/// <summary>
/// Builds test scenarios for event converter verification.
/// </summary>
public class EventConverterTestBuilder
{
    public EventConverterTestBuilder WithEvent(string name, string json) { ... }
    public EventConverterTestBuilder WithMalformedJson(string name) { ... }
    public EventConverterContext Build() { ... }
}

/// <summary>
/// Assertions for stream events in tests.
/// </summary>
public static class StreamEventAssertions
{
    public static void AssertIsFaulted(StreamEvent @event) { ... }
    public static void AssertIsUnknown(StreamEvent @event) { ... }
    public static T AssertIsEvent<T>(StreamEvent @event) { ... }
}
```

### 5c. Documentation

**Goal:** Developers can handle event evolution without reading source code.

#### Deliverables

| Document | Location | Content |
|----------|----------|---------|
| **Event Evolution Guide** | `docs/event-evolution.md` | Concepts, patterns, examples |
| **XML Doc Improvements** | Source files | Clarify null-return semantics |
| **Sample: Schema Migration** | `sample/` | Real-world rename + field addition |

#### Event Evolution Guide Outline

```markdown
# Event Evolution

## Overview
- Why events evolve
- Chronicles philosophy: streams never throw

## Patterns

### Pattern 1: Event Rename
- Using aliases
- Migration strategy

### Pattern 2: Field Addition
- JSON default values
- Nullable vs required

### Pattern 3: Custom Converter
- When to use
- Implementation template

### Pattern 4: Handling Unknown Events
- In projections
- In command handlers

## Testing
- Verifying converters
- Mixed-version streams

## FAQ
- Can I delete old events? (No)
- How do I know which events exist? (EventCatalog)
```

---

## 6. Open Questions for Maintainer

### Q1: Null Converter Return → UnknownEvent

**Current behavior:** If `IEventDataConverter.Convert()` returns `null`, the event becomes `UnknownEvent`.

**Question:** Is this intentional opt-out behavior, or should it be documented as an error?

**Team recommendation:** Keep current behavior. Document as "converter doesn't recognize this event name." This enables converters to be selective about which events they handle.

**Decision needed:** Confirm or override.

### Q2: Alias Conflict Behavior

**Question:** When an alias conflicts with another registration, should we:
- A) Throw `InvalidOperationException` at registration time (fail-fast)
- B) Last-registration-wins (silent override)
- C) First-registration-wins (silent ignore)

**Team recommendation:** Option A — `InvalidOperationException` with clear message:
```
Event name 'order-placed' is already registered. 
Registered by: OrderCreated (primary)
Attempted by: OrderPlaced (as alias)
```

**Decision needed:** Confirm approach.

### Q3: Documentation Scope

**Question:** Should `docs/event-evolution.md` include:
- A) Just Chronicles-specific patterns
- B) General ES/CQRS evolution theory + Chronicles specifics
- C) Link to external resources for theory, focus on Chronicles

**Team recommendation:** Option C — developers likely know ES basics; focus on "how to do X in Chronicles."

**Decision needed:** Confirm scope.

---

## 7. Deferred Features (v1.x)

The following features were discussed but explicitly excluded from v1.0. They may be added based on user feedback.

### Fluent Upcasting API

```csharp
// DEFERRED: Fluent transformation chain
builder.AddEvent<OrderCreated>("order-created")
    .WithAlias("order-placed")
    .WithUpcast(v1 => new OrderCreated(v1.OrderId, v1.Amount, Currency: "USD"));
```

**Exit criteria:** 3+ users request this pattern.

### Lambda Converters

```csharp
// DEFERRED: Inline converter without class
builder.AddEvent<OrderCreated>("order-created", 
    convert: ctx => ctx.Data.Deserialize<OrderCreated>(ctx.Options));
```

**Exit criteria:** Boilerplate complaints after aliases ship.

### Evolution Chain API

```csharp
// DEFERRED: Multi-version transformation
builder.AddEvent<OrderCreatedV3>("order-created")
    .FromV1<OrderCreatedV1>(v1 => ...)
    .FromV2<OrderCreatedV2>(v2 => ...);
```

**Exit criteria:** Users need 3+ version transformations.

### SchemaVersion on EventMetadata

```csharp
// DEFERRED: Explicit version discriminator
public record EventMetadata(
    string Name,
    int? SchemaVersion,  // NEW
    ...);
```

**Exit criteria:** Users adopt schema registries (Avro, Protobuf) requiring version tracking.

### Event Deprecation Support

```csharp
// DEFERRED: Mark events as deprecated
builder.AddEvent<OrderPlaced>("order-placed")
    .Deprecated("Use OrderCreated instead");
```

**Exit criteria:** Large teams request compile-time/runtime deprecation warnings.

---

## 8. Success Criteria

### v1.0 Release Criteria

| Criterion | Metric |
|-----------|--------|
| Multi-name API implemented | `AddEvent<T>(name, aliases)` merged |
| All edge case tests pass | 6 new tests green |
| Documentation published | `docs/event-evolution.md` exists |
| No breaking changes | Existing `AddEvent<T>(name)` unchanged |
| No new dependencies | Only BCL types used |

### Quality Gates

- [ ] All tests pass (`dotnet test -c Release`)
- [ ] No new warnings (`TreatWarningsAsErrors`)
- [ ] XML docs complete for new public API
- [ ] Sample code compiles and runs

### User Validation

Post-release, track:
- GitHub issues mentioning "event evolution" or "aliases"
- Requests for deferred features
- Confusion in documentation (indicates gaps)

---

## Appendix A: Code Examples

### A1: Complete Event Rename Migration

```csharp
// Step 1: Define the new event type
public record OrderCreated(
    string OrderId,
    decimal Amount,
    DateTimeOffset CreatedAt);

// Step 2: Register with alias
services.AddChronicles(chronicles => chronicles
    .AddEventStore("orders", store => store
        .AddEvent<OrderCreated>("order-created", aliases: "order-placed")
        .AddEvent<OrderShipped>("order-shipped")
        .AddEvent<OrderCancelled>("order-cancelled")));

// Step 3: Projection handles both old and new events identically
public class OrderProjection : IStateProjection<OrderState>
{
    public OrderState Apply(OrderState state, object @event)
        => @event switch
        {
            OrderCreated e => state with { Status = "Created", Amount = e.Amount },
            OrderShipped e => state with { Status = "Shipped" },
            OrderCancelled e => state with { Status = "Cancelled" },
            _ => state
        };
}
```

### A2: Custom Converter for Type Change

```csharp
// When JSON structure changes, custom converter is required
public class PaymentReceivedConverter : IEventDataConverter
{
    public object? Convert(EventConverterContext context)
    {
        if (context.Metadata.Name != "payment-received")
            return null;

        // Check for V1 structure (decimal Amount)
        if (context.Data.TryGetProperty("Amount", out var amountProp) 
            && amountProp.ValueKind == JsonValueKind.Number)
        {
            var amount = amountProp.GetDecimal();
            return new PaymentReceived(new Money(amount, "USD"));
        }

        // V2 structure (Money object)
        return context.Data.Deserialize<PaymentReceived>(context.Options);
    }
}

// Registration
builder.AddEvent<PaymentReceived>("payment-received", new PaymentReceivedConverter());
```

### A3: Handling Unknown/Faulted in Projections

```csharp
public class ResilientOrderProjection : IDocumentProjection<OrderDocument>
{
    private readonly ILogger<ResilientOrderProjection> logger;

    public OrderDocument Apply(OrderDocument doc, StreamEvent @event)
    {
        return @event.Data switch
        {
            OrderCreated e => ApplyOrderCreated(doc, e),
            OrderShipped e => ApplyOrderShipped(doc, e),
            
            // Handle evolution gracefully
            UnknownEvent u => HandleUnknown(doc, u, @event.Metadata),
            FaultedEvent f => HandleFaulted(doc, f, @event.Metadata),
            
            _ => doc
        };
    }

    private OrderDocument HandleUnknown(OrderDocument doc, UnknownEvent u, EventMetadata meta)
    {
        logger.LogWarning(
            "Unknown event '{Name}' at version {Version} in stream {StreamId}. " +
            "Consider upgrading to handle this event type.",
            meta.Name, meta.Version, meta.StreamId);
        return doc;
    }

    private OrderDocument HandleFaulted(OrderDocument doc, FaultedEvent f, EventMetadata meta)
    {
        logger.LogError(f.Exception,
            "Failed to deserialize '{Name}' at version {Version}. JSON: {Json}",
            meta.Name, meta.Version, f.Json);
        return doc;
    }
}
```

### A4: Testing Mixed-Version Streams

```csharp
[Fact]
public async Task Projection_HandlesMixedVersionStream()
{
    // Arrange: Stream with old and new event names
    var stream = await eventStore.GetStreamAsync(streamId);
    
    // Simulate historical events (would exist in real DB)
    // Event 1: "order-placed" (old name)
    // Event 2: "order-created" (new name)
    
    var projection = new OrderProjection();
    var state = OrderState.Empty;

    // Act
    foreach (var @event in stream.Events)
    {
        state = projection.Apply(state, @event.Data);
    }

    // Assert: Both events processed correctly
    Assert.Equal("Created", state.Status);
}
```

---

## Appendix B: Implementation Checklist

### EventStoreBuilder Changes

- [ ] Add `AddEvent<TEvent>(string name, params string[] aliases)` overload
- [ ] Create `AliasedEventDataConverter` internal class
- [ ] Add conflict detection in `Build()` method
- [ ] Update XML documentation

### EventCatalog Changes

- [ ] Accept alias mappings in constructor
- [ ] Register aliases pointing to primary converter
- [ ] No changes to `GetConverter()` — aliases are transparent

### Test Additions

- [ ] `EventDataConverterTests.cs` — null return, malformed JSON
- [ ] `EventCatalogTests.cs` — alias registration, conflict detection
- [ ] `StreamEventConverterTests.cs` — mixed-version integration

### Documentation

- [ ] Create `docs/event-evolution.md`
- [ ] Update `docs/event-store.md` with evolution section
- [ ] Add XML docs to new `AddEvent` overload

---

*End of PRD*
