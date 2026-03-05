# API Design Decisions — v1.0.0

**Lead:** Thufir  
**Date:** 2026-03-04  
**Context:** Resolving 10 design concerns from Duncan's EventStore review for v1.0.0 release.

---

## Decision 1: EventId in EventMetadata
**Decision:** Accept for v1.0.0  
**Rationale:** Idempotency and deduplication are essential for at-least-once delivery scenarios. Adding `string? EventId` now avoids a breaking change later (adding a required parameter) and enables critical production patterns.  
**Implementation:** Add `string? EventId = null` as an optional property to the `EventMetadata` record. Writer infrastructure can auto-generate GUIDs when null. Consumers can use EventId for deduplication.

---

## Decision 2: SchemaVersion in EventMetadata
**Decision:** Defer to v1.x (additive)  
**Rationale:** Schema versioning is valuable for upcasting, but Chronicles can ship without it. Event type names already encode version implicitly (e.g., `OrderCreatedV2`). Adding `int? SchemaVersion` later is non-breaking. Lower priority than EventId.

---

## Decision 3: CreatedAt in StreamMetadata
**Decision:** Defer to v1.x (additive)  
**Rationale:** `Timestamp` (last updated) exists. `CreatedAt` is a nice-to-have for queries but not essential for core event sourcing. Adding a nullable `DateTimeOffset? CreatedAt` later is non-breaking.

---

## Decision 4: expectedVersion guard on DeleteStreamAsync
**Decision:** Accept for v1.0.0  
**Rationale:** Delete without version guard is unsafe for concurrent scenarios. This is a dangerous omission. Adding `StreamVersion? expectedVersion = null` as optional parameter is backward-compatible and provides safety when needed.  
**Implementation:** Add optional `StreamVersion? expectedVersion = null` parameter to `DeleteStreamAsync`. When provided, verify stream version before deletion; throw `StreamConflictException` on mismatch.

---

## Decision 5: Rename ICommandHandler<TCommand> → IStreamCommandHandler<TCommand>
**Decision:** Defer to v2 (breaking)  
**Rationale:** While the naming is confusing (`ICommandHandler<TCommand>` has `ConsumeEvent` so it's stateful, yet `IStatelessCommandHandler` is the truly stateless variant), renaming is a breaking change that would disrupt all existing implementations. The current names work — they're just suboptimal. Document the distinction clearly instead.

---

## Decision 6: TCommand param in ConsumeEvent
**Decision:** Doc-only  
**Rationale:** The `TCommand` parameter in `ICommandHandler<TCommand>.ConsumeEvent(StreamEvent, TCommand, IStateContext)` allows command-specific projections (e.g., filtering events based on command context). This is intentional — it enables optimized partial projections where only events relevant to the command are processed. Document this design rationale.  
**Documentation:** Add XML doc explaining that TCommand enables command-scoped projections for performance optimization in handlers that don't need full aggregate state.

---

## Decision 7: CloseAsync / Archived state — implement or remove
**Decision:** Accept for v1.0.0 (implement CloseAsync only)  
**Rationale:** `CloseAsync` is in the public interface with a TODO stub — shipping this incomplete is unacceptable. Either implement or remove. Since stream closing is a useful lifecycle feature (prevent writes after aggregate completion), implement it. `Archived` state and `ArchiveStreamAsync` remain deferred (commented out) as they require additional infrastructure.  
**Implementation:** Complete `CloseAsync` implementation: update stream metadata to `StreamState.Closed`, persist to Cosmos. Leave `Archived` state and `ArchiveStreamAsync` commented out for v1.x.

---

## Decision 8: IQueryHandler<TQuery, TResult> abstraction
**Decision:** Defer to v1.x (additive)  
**Rationale:** Chronicles' read-side is intentionally flexible — users query Cosmos directly or use their own patterns. Adding `IQueryHandler` is additive and optional. Let the library ship without prescribing a query pattern; add it in v1.x if users request it.

---

## Decision 9: Extend IEventSubscriptionExceptionHandler with StreamEvent + CancellationToken
**Decision:** Accept for v1.0.0  
**Rationale:** Without event context, exception handlers cannot implement dead-letter queues or meaningful diagnostics. This is a breaking change but we're pre-release — better to fix the signature now than ship an incomplete API.  
**Implementation:** Change signature to `ValueTask HandleAsync(Exception exception, StreamEvent? evt, CancellationToken cancellationToken)`. Existing implementations will need to update — acceptable for pre-release.

---

## Decision 10: Document read-side architecture
**Decision:** Doc-only  
**Rationale:** This is not a code change — it's documentation. Chronicles' read-side uses direct Cosmos document access via `IDocumentProjection` and the document store. This is intentional and should be documented.  
**Documentation:** Add architecture doc explaining: (1) projections write to Cosmos via `IDocumentProjection`, (2) queries read directly from Cosmos document store, (3) no explicit `IQueryHandler` by design — users choose their own query patterns.

---

## Summary

### Accepted for v1.0.0 (4 items)
1. **EventId in EventMetadata** — idempotency support (additive)
4. **expectedVersion guard on DeleteStreamAsync** — concurrency safety (additive)
7. **Implement CloseAsync** — complete the stub (additive behavior fix)
9. **Extend IEventSubscriptionExceptionHandler** — add event context (breaking, pre-release OK)

### Deferred (4 items)
- **Decision 2** (SchemaVersion) → v1.x additive
- **Decision 3** (CreatedAt) → v1.x additive
- **Decision 5** (Rename ICommandHandler) → v2 breaking
- **Decision 8** (IQueryHandler) → v1.x additive

### Doc-only (2 items)
- **Decision 6** — Document TCommand param intent in ConsumeEvent
- **Decision 10** — Document read-side architecture

### Tasks for Gurney
1. Add `string? EventId = null` to `EventMetadata` record
2. Add `StreamVersion? expectedVersion = null` parameter to `DeleteStreamAsync` with validation
3. Complete `CloseAsync` implementation in `EventStreamWriter`
4. Extend `IEventSubscriptionExceptionHandler.HandleAsync` signature with `StreamEvent?` and `CancellationToken`

### Documentation Tasks (Thufir or Duncan)
- Document `TCommand` parameter rationale in `ICommandHandler<TCommand>.ConsumeEvent` XML docs
- Write architecture note on read-side patterns (direct Cosmos access)
