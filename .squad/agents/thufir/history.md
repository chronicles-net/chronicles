# Thufir — History

## Project Context
- **Project:** Chronicles — event sourcing + CQRS framework for .NET 10
- **Stack:** C#, .NET 10, Azure Cosmos DB (v3.55.0), Aspire, xunit.v3, OpenTelemetry
- **Repo:** chronicles-net/chronicles
- **User:** Lars Skovslund
- **Joined:** 2026-03-04

## Core Context
Chronicles provides abstractions for event streams (IEventStreamReader/Writer), document projections (IDocumentProjection → Cosmos DB), and CQRS command handling (ICommandHandler, ICommandProcessor). Sample microservices (Order, Restaurant, Courier) demonstrate real-world usage via Aspire orchestration.

## Learnings

### 2026-03-04 — API Design Decisions for v1.0.0

**Decisions made:** Resolved 10 design concerns from Duncan's EventStore review.

**Accepted for v1.0.0 (4):**
- `EventId` in `EventMetadata` — critical for idempotency/dedup
- `expectedVersion` guard on `DeleteStreamAsync` — unsafe without it
- Complete `CloseAsync` implementation — can't ship a TODO stub
- Extend `IEventSubscriptionExceptionHandler` with event context — needed for dead-letter

**Deferred (4):**
- `SchemaVersion` → v1.x (event naming suffices for now)
- `CreatedAt` in `StreamMetadata` → v1.x (nice-to-have)
- Rename `ICommandHandler<TCommand>` → v2 (breaking, low value)
- `IQueryHandler<TQuery,TResult>` → v1.x (users handle queries directly)

**Doc-only (2):**
- `TCommand` param in `ConsumeEvent` — intentional for command-scoped projections
- Read-side architecture — document direct Cosmos access pattern

**Principle applied:** Pre-release breaking changes are acceptable; post-release, defer or use additive overloads. When uncertain, defer.

### 2026-03-05 — Layer Architecture Audit

**Directive:** Documents → EventStore → CQRS strict layering (Lars, via coordinator directive). Higher layers may only use **public** types from lower layers. Any exception must be explicitly documented.

**Structure:** Single assembly (`Chronicles.csproj`). All layers are namespace folders — `internal` is assembly-wide, not layer-scoped. The `.Internal` namespace is a naming convention with zero compiler enforcement.

**Violations found (3 files):**
1. `EventStore/DependencyInjection/EventStoreBuilder.cs` → uses `Chronicles.Documents.Internal` types (`IChangeFeedFactory`, `DocumentSubscription`, `IDocumentSubscription`) for change-feed subscription wiring
2. `Cqrs/Internal/CommandProcessor.cs` → uses `Chronicles.EventStore.Internal.StateContext` (instantiates directly)
3. `Cqrs/Internal/DocumentProjectionRebuilder.cs` → uses `Chronicles.EventStore.Internal.StateContext` (instantiates directly)

**Compliant areas:** EventStore → Documents public types ✅, Cqrs → EventStore public types ✅, Cqrs → Documents public types ✅ (permitted by directive).

**Recommendation:** Split into three projects (`Chronicles.Documents`, `Chronicles.EventStore`, `Chronicles.Cqrs`) to get compiler-enforced boundaries. This is the right time pre-1.0. Interim: add `NetArchTest` architecture tests to prevent further violations.

**Immediate fixes needed:**
- `StateContext`: Expose a public factory method on `IStateContext` or make constructor public
- Change-feed subscription: Promote subscription factory API to public in Documents, or redesign the EventStore registration pattern

**Report:** `.squad/decisions/inbox/thufir-layer-audit.md`
