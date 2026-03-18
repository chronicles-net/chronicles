# Event Evolution — Design Record

**Author:** Duncan Idaho (ES/CQRS Expert)  
**Status:** Shipped — Implemented in v1.0  
**Originally drafted:** 2026-03-05  
**Reconciled to implementation:** 2026-03-25  
**Scope:** Chronicles event rename support, deserialization safety, documentation, and validation coverage

## Summary

This document began as the v1 proposal for event evolution support. The core scope described here is now implemented in the codebase:

- `EventStoreBuilder` supports `AddEvent<TEvent>(string name, params string[] aliases)` for backwards-compatible event renames.
- `EventCatalog` accepts optional alias mappings and resolves legacy names during deserialization.
- `EventStoreBuilder.Build()` validates primary names and aliases and throws `InvalidOperationException` on conflicts.
- `IEventDataConverter` explicitly documents that returning `null` produces `UnknownEvent`.
- `docs/event-evolution.md` is published as the user-facing guide.
- Unit tests cover alias registration, alias lookup, conflict detection, null-return handling, and deserialization fault wrapping.

The original draft also proposed a few implementation details that did not survive intact. In particular, Chronicles did **not** need a dedicated `AliasedEventDataConverter`, did **not** add bespoke test-helper types for this feature, and does **not** ship a standalone migration sample under `sample\`. Those items are corrected below so this document remains historically useful.

---

## 1. Context and Problem Statement

Event-sourced systems accumulate events over years. Business requirements change, domain language evolves, and event schemas must adapt without rewriting stored history.

The scenarios that motivated this work remain valid:

- **Rename**: `OrderPlaced` → `OrderCreated`
- **Field addition**: add `Currency` to `PaymentReceived`
- **Type change**: evolve `Amount` from `decimal` to a value object such as `Money`
- **Forward compatibility**: older readers encounter newer event names or shapes

Chronicles already had the right low-level deserialization model for most of this work:

- unknown names become `UnknownEvent`
- converter failures become `FaultedEvent`
- stream reads continue rather than crashing the entire replay path

The main v1 friction was simple renames. Before aliases shipped, even a pure name change required a custom `IEventDataConverter`. The goal of the v1 work was to make rename scenarios cheap while keeping structural changes explicit through custom converters.

---

## 2. Shipped Architecture

### Deserialization Pipeline

```
┌─────────────────┐    ┌─────────────────┐    ┌──────────────┐    ┌────────────────────┐
│ Cosmos DB JSON  │───▶│ StreamEvent-    │───▶│ EventCatalog │───▶│ IEventDataConverter│
│                 │    │ Converter       │    │ (lookup)     │    │ (Convert)          │
└─────────────────┘    └─────────────────┘    └──────────────┘    └────────────────────┘
                                                     │                      │
                                                     ▼                      ▼
                                              UnknownEvent            FaultedEvent
                                              (no converter or         (exception during
                                              converter returns null)  lookup/conversion)
```

### Key Types

| Type | Current role |
|------|--------------|
| `StreamEvent` | Pairs converted event data with `EventMetadata` |
| `UnknownEvent` | Wraps raw JSON when no converter handles the event |
| `FaultedEvent` | Wraps raw JSON and an exception when conversion fails |
| `IEventDataConverter` | Converts `EventConverterContext` into an event instance or `null` |
| `EventConverterContext` | Carries raw `JsonElement`, metadata, and serializer options |
| `EventCatalog` | Resolves event names to converters and types to canonical names |
| `EventStoreBuilder` | Registers primary event names, aliases, custom converters, and the event catalog |

### Current Conversion Behavior

The implementation in `StreamEventConverter` is intentionally defensive:

```csharp
public StreamEvent Convert(EventConverterContext context)
{
    try
    {
        return new(
            eventCatalog.GetConverter(context.Metadata.Name)?.Convert(context)
            ?? new UnknownEvent(context.Data.GetRawText()),
            context.Metadata);
    }
    catch (Exception ex)
    {
        return new(
            new FaultedEvent(context.Data.GetRawText(), ex),
            context.Metadata);
    }
}
```

That behavior is now both implemented and tested. The event stream layer treats unknown or malformed payloads as data problems to surface to projections, not as reasons to abort every read.

---

## 3. Shipped v1 Scope

### 3a. Multi-Name Registration API

The v1 API shipped as a new overload on `EventStoreBuilder`:

```csharp
public EventStoreBuilder AddEvent<TEvent>(
    string name,
    params string[] aliases)
    where TEvent : class
```

#### What it enables

```csharp
builder.AddEvent<OrderCreated>("order-created", "order-placed");
```

This allows new writes to use `order-created` while still recognizing historical `order-placed` events during deserialization.

#### Actual implementation details

1. **Primary name is canonical for writes.** `GetEventName(Type)` still returns the primary registration only.
2. **Aliases are read-only.** They exist for deserialization compatibility and are never written back out.
3. **Conflict detection happens in `Build()`.** `ValidateEventNames()` checks the full set of primary names and aliases and throws `InvalidOperationException` if any name is duplicated.
4. **No dedicated alias converter class was added.** The implementation uses `EventDataConverter(name, typeof(TEvent))` for the primary registration and creates additional `EventDataConverter(alias, typeof(TEvent))` instances for alias registrations.
5. **Re-registering an event type replaces prior aliases.** The builder clears previous alias registrations for the same `TEvent` before adding new ones.
6. **Custom catalogs still override builder registrations.** If `AddEventCatalog<TCatalog>()` is used, the default catalog built from `AddEvent(...)` registrations is skipped.

#### Why the implementation differs from the draft

The original draft proposed a dedicated `AliasedEventDataConverter`. The shipped code did not need that extra type. Separate default converters per registered name were simpler, preserved existing behavior, and kept the feature additive.

### 3b. Validation and Test Coverage

The original draft described test coverage as a major gap. That statement is no longer true for the v1 core scope.

#### Tests that exist today

| Area | Evidence in current tests |
|------|---------------------------|
| Alias conflict detection | `EventStoreBuilderTests`: conflicting alias vs primary, duplicate alias, no-conflict success |
| Alias lookup in catalog | `EventCatalogTests`: alias lookup, canonical name retention, primary/alias retrieval |
| Converter returns `null` | `StreamEventConverterTests.Convert_Should_Return_Unknown_Event_When_Converter_Returns_Null` |
| Converter throws | `StreamEventConverterTests.Convert_Should_Return_FaultedEvent_On_Converter_Exception` |
| Catalog throws during lookup | `StreamEventConverterTests.Convert_Should_Return_FaultedEvent_On_EventCatalog_Exception` |
| Unknown event name | `StreamEventConverterTests.Convert_Should_Return_Unknown_Event_When_EventName_Is_Unknown` |

#### What changed from the draft

- The v1 design no longer needs proposed helper abstractions like `EventConverterTestBuilder` or `StreamEventAssertions`.
- Standard xUnit + FluentAssertions style is sufficient for the current scenarios.
- The critical behavior around `null` returns and alias conflicts is covered already.

#### Remaining low-priority coverage opportunities

The following still make sense as follow-up coverage if someone wants deeper confidence, but they are not part of the shipped v1 scope:

- a syntax-level malformed JSON test that proves `FaultedEvent` wrapping from a broken payload shape, not just thrown converters
- a fuller mixed-version integration test that exercises aliases through a stream-oriented scenario rather than focused unit tests
- an explicit `JsonValueKind.Null` payload test if that boundary condition becomes important in practice

### 3c. Documentation Deliverables

The documentation work also shipped, with one correction to the original proposal.

| Deliverable | Status | Notes |
|-------------|--------|-------|
| `docs/event-evolution.md` | ✅ Shipped | Chronicles-focused guide with rename, field-addition, custom-converter, and unknown/faulted-event patterns |
| XML doc clarification on `IEventDataConverter` | ✅ Shipped | `null` return is documented as "converter does not handle this event name" |
| Standalone sample under `sample\` | ❌ Not part of shipped scope | No dedicated migration sample is confirmed in the current `sample\` tree |

The proposal originally promised a standalone sample. That did not materialize, and this document should not keep claiming otherwise. The guide in `docs/event-evolution.md` is the shipped documentation surface.

---

## 4. Resolved Questions from the Original Draft

### Q1. Is `null` from `IEventDataConverter` intentional?

**Resolved:** Yes.

The public XML documentation on `IEventDataConverter.Convert()` now states that returning `null` means the converter does not handle the event name and that the event will be wrapped as `UnknownEvent`. `StreamEventConverterTests` verifies this behavior.

### Q2. How do alias conflicts behave?

**Resolved:** Chronicles fails during `EventStoreBuilder.Build()` with `InvalidOperationException`.

This is the shipped behavior for duplicate primary names, alias-vs-primary collisions, and alias-vs-alias collisions. The implementation is deterministic and avoids silent overrides.

### Q3. What scope should the documentation have?

**Resolved:** Chronicles-specific guidance with links out for general theory.

`docs/event-evolution.md` focuses on how to perform rename, field-addition, custom-converter, and tolerant projection scenarios in Chronicles. It links to external event-sourcing material rather than trying to duplicate broad theory.

---

## 5. Technical Corrections to the Original Proposal

The original PRD captured the right product direction, but several draft assumptions were superseded by the actual implementation.

### Removed or corrected assumptions

1. **`AliasedEventDataConverter`**  
   Not implemented and not needed. Alias support is handled by registering per-name `EventDataConverter` instances and wiring them into `EventCatalog`.

2. **Dedicated event-converter test helpers**  
   Not implemented. Current tests use ordinary xUnit patterns and remain readable without extra framework surface.

3. **"Critical test infrastructure gaps" framing**  
   Outdated. The essential alias and null-return behaviors are now covered by the existing test suite.

4. **Standalone `sample\` migration example**  
   Not shipped. The canonical documentation lives in `docs/event-evolution.md`.

### Still-valid design guidance

The parts of the proposal that remain accurate are the most important ones:

- aliases are the right abstraction for simple renames
- field additions are usually handled by normal JSON defaults
- structural/type changes should stay explicit through `IEventDataConverter`
- unknown and faulted events should be surfaced to projections instead of crashing stream reads

---

## 6. Follow-Up Notes After v1

These are genuine follow-up opportunities, not missing core features.

1. **Deeper integration coverage**  
   Add a mixed-version stream test if the team wants a higher-level proof that aliases behave correctly through a more realistic read model or replay path.

2. **Boundary-payload coverage**  
   Add explicit tests for malformed JSON syntax or `null` payload shapes if future regressions suggest value in locking those down.

3. **Sample strategy**  
   If a long-lived sample app under `sample\` needs to demonstrate a rename or custom converter, add it there deliberately. Until then, the guide in `docs/event-evolution.md` should remain the documented entry point.

---

## 7. Deferred Roadmap (Still Sensible Post-v1)

The following ideas were explicitly out of v1 scope and still belong in roadmap territory rather than the shipped baseline.

### Fluent Upcasting API

```csharp
builder.AddEvent<OrderCreated>("order-created")
    .WithAlias("order-placed")
    .WithUpcast(v1 => new OrderCreated(v1.OrderId, v1.Amount, Currency: "USD"));
```

Useful only if consumers repeatedly ask for chained transformations beyond the current alias + custom converter model.

### Lambda Converters

```csharp
builder.AddEvent<OrderCreated>(
    "order-created",
    convert: ctx => ctx.Data.Deserialize<OrderCreated>(ctx.Options));
```

Potentially ergonomic, but not necessary while converter classes remain straightforward and explicit.

### Evolution Chain API

```csharp
builder.AddEvent<OrderCreatedV3>("order-created")
    .FromV1<OrderCreatedV1>(v1 => ...)
    .FromV2<OrderCreatedV2>(v2 => ...);
```

Worth reconsidering only if real consumers start maintaining longer event-version chains.

### Schema Version on `EventMetadata`

```csharp
public record EventMetadata(
    string Name,
    int? SchemaVersion,
    ...);
```

Still a plausible future enhancement, but it is not required for the current alias-based rename strategy.

### Event Deprecation Support

```csharp
builder.AddEvent<OrderPlaced>("order-placed")
    .Deprecated("Use order-created instead");
```

Interesting for larger teams, but unnecessary for the shipped core feature set.

---

## 8. Release Outcome

### Core v1 Criteria

| Criterion | Outcome |
|-----------|---------|
| Multi-name API implemented | ✅ `AddEvent<TEvent>(string name, params string[] aliases)` exists |
| Alias-aware catalog behavior | ✅ `EventCatalog` accepts optional alias mappings |
| Conflict detection | ✅ `Build()` rejects duplicate primary names and aliases |
| Null-return behavior documented | ✅ `IEventDataConverter` XML docs updated |
| User-facing documentation published | ✅ `docs/event-evolution.md` exists |
| Existing registration APIs preserved | ✅ `AddEvent<TEvent>(string)` and custom-converter overload remain intact |
| No new package dependencies required | ✅ Alias support uses existing infrastructure |

### Quality Readout

| Check | Outcome |
|-------|---------|
| Alias conflict tests present | ✅ Yes |
| Alias lookup tests present | ✅ Yes |
| `UnknownEvent` null-return test present | ✅ Yes |
| Fault wrapping tests present | ✅ Yes |
| Standalone migration sample present | ❌ No — not part of current shipped scope |

### Historical takeaway

The original proposal was directionally correct. The shipped implementation landed the important user value with less machinery than first expected.

---

## Appendix A: Representative Patterns

### A1. Event Rename with Alias

```csharp
public record OrderCreated(
    string OrderId,
    decimal Amount,
    DateTimeOffset CreatedAt);

services.AddChronicles(builder => builder
    .WithEventStore("orders", store => store
        .AddEvent<OrderCreated>("order-created", "order-placed")
        .AddEvent<OrderShipped>("order-shipped")
        .AddEvent<OrderCancelled>("order-cancelled")));
```

Use this when the event name changed but the payload model is still compatible.

### A2. Custom Converter for Structural Evolution

```csharp
public class PaymentReceivedConverter : IEventDataConverter
{
    public object? Convert(EventConverterContext context)
    {
        if (context.Metadata.Name != "payment-received")
            return null;

        if (context.Data.TryGetProperty("Amount", out var amountProp)
            && amountProp.ValueKind == JsonValueKind.Number)
        {
            var amount = amountProp.GetDecimal();
            return new PaymentReceived(new Money(amount, "USD"));
        }

        return context.Data.Deserialize<PaymentReceived>(context.Options);
    }
}

builder.AddEvent<PaymentReceived>(
    "payment-received",
    new PaymentReceivedConverter());
```

Use this when the event name can stay the same but the payload structure no longer deserializes cleanly into the current type.

### A3. Tolerant Projection Behavior

```csharp
public OrderDocument Apply(OrderDocument document, StreamEvent @event)
    => @event.Data switch
    {
        OrderCreated e => ApplyOrderCreated(document, e),
        UnknownEvent _ => document,
        FaultedEvent _ => document,
        _ => document
    };
```

The important rule is that projections should remain tolerant of unknown or faulted data and surface diagnostics through logging or monitoring instead of halting replay.

---

## Appendix B: Implementation Record

### Shipped Work

- [x] Add `AddEvent<TEvent>(string name, params string[] aliases)` overload
- [x] Validate primary names and aliases in `EventStoreBuilder.Build()`
- [x] Extend `EventCatalog` with optional alias mappings
- [x] Clarify `IEventDataConverter` null-return semantics in XML docs
- [x] Publish `docs/event-evolution.md`
- [x] Add or retain unit coverage for alias lookup, alias conflicts, unknown events, and fault wrapping

### Explicitly Not Shipped as Part of v1

- [ ] Dedicated `AliasedEventDataConverter` type
- [ ] Dedicated event-converter test-helper library
- [ ] Standalone migration sample under `sample\`

### Follow-Up Candidates

- [ ] Add malformed JSON syntax coverage if needed
- [ ] Add a mixed-version integration test if higher-level validation becomes valuable
- [ ] Add a maintained sample only when a concrete sample application needs the scenario

---

*End of design record*
