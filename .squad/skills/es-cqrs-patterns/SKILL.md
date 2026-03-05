---
name: "es-cqrs-patterns"
description: "Event Sourcing and CQRS design patterns and anti-patterns observed in Chronicles"
domain: "event-sourcing"
confidence: "high"
source: "duncan eventstore review 2026-03-04"
---

## Context

Patterns and anti-patterns derived from reviewing the Chronicles EventStore + CQRS layer. Chronicles uses Cosmos DB as the backing store, with StreamId/StreamVersion as event identity primitives, and a typed projection pipeline from IStateProjection<TState> → IDocumentProjection<TDocument>.

---

## Patterns

### StreamVersion Sentinel Design

Chronicles uses negative long values as sentinels on `StreamVersion` to express write intent:
- `RequireEmpty = -1` → stream must have zero events
- `RequireNotEmpty = -2` → stream must have at least one event
- `Any = 0` → no version check

**Pattern:** Express write intent (not just numeric position) via named sentinel instances on the version type. Keep them as `static readonly` fields on the struct, not a separate enum, to maintain type compatibility with real versions.

**Anti-pattern:** Using `0` as the sentinel for "any" when `0` also means "empty stream version". These semantics collide and break `IsEmpty` / `IsAny` disambiguation. Use a clearly out-of-range value (e.g., `long.MinValue` or `-3`) for "any".

---

### Handler-as-Projection (Aggregate-Within-Handler)

`ICommandHandler<TCommand, TState>` inherits `IStateProjection<TState>`. The command handler is responsible for:
1. Rebuilding its own aggregate state (`CreateState`, `ConsumeEvent`)
2. Executing the command against that state (`ExecuteAsync`)

**Pattern:** Keeping state reconstruction and command execution co-located in a single class reduces indirection and makes the aggregate boundary explicit. Use when state is always stream-scoped and always needs full event replay.

**Anti-pattern:** Putting the current command (`TCommand`) as a parameter on `ConsumeEvent` during state replay. State reconstruction must be command-agnostic — the same events should produce the same state regardless of what command is currently being processed.

---

### Fluent Command Context Builder

`CommandContextExtensions` provides `AddEventWhen`, `AddEventWhen<TState>`, `WithStateResponse`, `WithResponse` as extension methods on `ICommandContext<TCommand>`. This allows handler bodies to be written as fluent pipelines:

```csharp
context
    .AddEventWhen(given, when, then)
    .WithStateResponse(this);
```

**Pattern:** Chain `ICommandContext<TCommand>` mutating operations via extensions that return `this`. Keeps handler code declarative and readable.

**Critical anti-pattern / BUG:** When capturing an event for both adding to the stream AND using in a response, always capture the factory result ONCE before calling `AddEvent`:

```csharp
// ❌ WRONG — factory called twice, two events written
var evt = factory(context);
context.AddEvent(factory(context));

// ✅ CORRECT — factory called once
var evt = factory(context);
context.AddEvent(evt);
```

---

### FaultedEvent / UnknownEvent Safe Deserialization

Events that fail to deserialize are wrapped in `FaultedEvent(string Json, Exception? Exception)` or `UnknownEvent(string Json)` rather than throwing. Processors receive these as `StreamEvent.Data` and can pattern match to handle gracefully.

**Pattern:** Never throw on unknown/undeserializable events during stream reading. Surface them as typed sentinel objects. Allows projection processors to skip or dead-letter individual events without crashing the entire stream read.

---

### Checkpoint Pattern for Resumable Processors

`Checkpoint<TState>` pairs a `StreamVersion` position with typed processor state. Writers call `IEventStreamWriter.SetCheckpointAsync(name, streamId, version, state)`, readers retrieve via `IEventStreamReader.GetCheckpointAsync<TState>(name, streamId)`.

**Pattern:** Named checkpoints (not anonymous) allow multiple independent processors to maintain their own positions on the same stream. Typed state avoids separate state storage for resumable logic.

---

### DocumentCommitAction as Projection Output Signal

`IDocumentProjection<TDocument>.OnCommitAsync` returns `DocumentCommitAction` (`Update` / `Delete` / `None`). This decouples the projection logic from the persistence action.

**Pattern:** Have the projection decide what to DO with the document (update, delete, skip), not the infrastructure. This enables soft-delete and conditional-write scenarios without framework changes.

---

### CommandConsistency for Write Isolation Levels

`CommandConsistency.ReadWrite` (default) appends events with an optimistic check that the stream hasn't changed since it was read. `CommandConsistency.Write` appends blindly (useful for append-only / insert-only scenarios like logging).

**Pattern:** Expose isolation level as a first-class option on the command, not buried in infrastructure. Let the domain decide its consistency requirements per-command.

---

## Anti-Patterns

### Missing Event Identity (No EventId)

`EventMetadata` carries `CorrelationId` and `CausationId` but no per-event unique ID. In at-least-once delivery (change feed, retries), projections cannot detect duplicate events. Always include a stable, writer-assigned `EventId` in event metadata for idempotent projection replay.

### No Schema Version on Events

Relying on event `Name` alone for upcasting is fragile. When a schema evolves, distinguish `OrderPlaced/v1` from `OrderPlaced/v2` with an explicit `SchemaVersion` field in `EventMetadata`, not naming conventions in `IEventDataConverter` implementations.

### Incomplete Stream Lifecycle in Public Interface

Exposing `CloseAsync` and `StreamState.Archived` in the public API before they are implemented creates a broken contract. Stub implementations (`// TODO`) in released interfaces erode trust. Either implement the feature fully or remove it from the public surface until ready.

### Delete Without Guard

`DeleteStreamAsync` with no version or state guard is a concurrency trap. Any delete of a mutable aggregate stream should accept an expected version to prevent silent data loss from racing writes.

### Exception Handler Without Event Context

`IEventSubscriptionExceptionHandler.HandleAsync(Exception)` strips all event context. A dead-letter handler that cannot identify WHICH event failed cannot support selective replay or monitoring. Always include stream ID, event name, and version in the exception handler contract.
