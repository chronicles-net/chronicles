# Duncan — History

## Project Context
- **Project:** Chronicles — event sourcing + CQRS framework for .NET 10
- **Stack:** C#, .NET 10, Azure Cosmos DB (v3.55.0), Aspire, xunit.v3, OpenTelemetry
- **Repo:** chronicles-net/chronicles
- **User:** Lars Skovslund
- **Joined:** 2026-03-04

## Core Context
Event flow in Chronicles: `IEventStreamWriter` appends `StreamEvent` to Cosmos DB. `IEventProcessor` reads the stream and applies events to state via `IStateProjection<TState>`. Document projections (`IDocumentProjection<TDocument>`) persist read models. CQRS: `ICommandHandler<TCommand>` is the write-side; projections are the read-side. `StreamMetadata` carries stream-level context.

## Learnings

### 2026-03-04 — EventStore Full Design Review

**Interfaces reviewed:** `IEventStreamWriter`, `IEventStreamReader`, `IEventProcessor`, `IEventStreamProcessor`, `IEventCatalog`, `IEventDataConverter`, `ICommandHandler<TCommand,TState>`, `ICommandHandler<TCommand>`, `IStatelessCommandHandler<TCommand>`, `IStateProjection<TState>`, `IDocumentProjection<TDocument>`, `IStateConsumer<TState>`, `ICommandProcessor<TCommand>`, `ICommandExecutor<TCommand>`, `ICommandContext<TCommand>`, `ICommandCompletionContext<TCommand>`, `IDocumentProjectionRebuilder<TProjection,TDocument>`, `IDocumentPublisher<TDocument>`

**Types reviewed:** `StreamId`, `StreamVersion`, `StreamEvent`, `EventMetadata`, `StreamMetadata`, `StreamState`, `StreamWriteOptions`, `StreamReadOptions`, `StreamOptions`, `StreamWriteResult`, `CommandResult`, `CommandOptions`, `CommandConsistency`, `CommandConflictBehavior`, `Checkpoint<TState>`, `FaultedEvent`, `UnknownEvent`, `ResultType`, `IStateContext`/`StateContext`, `EventStoreOptions`, `StreamConflictException`, `EventConverterContext`

**Key design patterns observed:**
- `StreamId` uses `Category.Id` dot-separated string; supports composite IDs via protected constructor
- `StreamVersion` uses sentinel long values: `RequireEmpty=-1`, `RequireNotEmpty=-2`, `Any=0`
- `ICommandHandler<TCommand,TState>` inherits `IStateProjection<TState>` — the handler IS its own aggregate state rebuilder
- `CommandContextExtensions` provides a fluent builder pattern for `AddEvent`/`WithStateResponse`
- `FaultedEvent` and `UnknownEvent` used for safe deserialization fallbacks
- Internal `EventStreamWriter` does up-to-5 silent retries on `StreamConflictException` when `RequiredVersion == Any`
- `IDocumentProjection<TDocument>` adds `OnCommitAsync` commit action hook over `IStateProjection`
- `Checkpoint<TState>` carries typed state alongside a version position — used for resumable processors

**Gaps and bugs found:**
1. **CRITICAL BUG:** `CommandContextExtensions.AddEventWhen` (two overloads with `respondWith`) calls the event factory delegate TWICE — adds a duplicate event to the stream while capturing a separate instance for the response.
2. **Semantic gap:** `StreamVersion.AnyValue == 0` conflates "don't check version" with "empty stream" (version 0). `IsEmpty` and `IsAny` both return `true` for value `0`.
3. **`EnsureSuccess` flaw:** Uses raw equality `metadata.Version != requiredVersion`. If `RequiredVersion = Any (0)` is set in options and stream is at version 5, it incorrectly throws. Should use `IsValid()` instead.
4. **No event ID / idempotency key** in `EventMetadata` — cannot detect duplicate writes in at-least-once delivery.
5. **No schema version** in `EventMetadata` — upcasting/versioning relies on naming alone.
6. **No `CreatedAt`** in `StreamMetadata` — only `Timestamp` (last updated).
7. **`DeleteStreamAsync` has no version guard** — unsafe for concurrent scenarios.
8. **`ICommandHandler<TCommand>.ConsumeEvent` takes `TCommand` as parameter** — state rebuild should be command-agnostic.
9. **`CloseAsync` is not implemented** — has a `// TODO: Implement close stream` in `EventStreamWriter`.
10. **`StreamState.Archived` has no write transition** — `ArchiveStreamAsync` is commented out in the writer interface.
11. **Naming confusion:** `ICommandHandler<TCommand>` (without TState) is NOT stateless — it has `ConsumeEvent`. `IStatelessCommandHandler<TCommand>` IS stateless. The names are misleading.
12. **No query-side abstraction** — CQRS "Q" is handled implicitly via Cosmos reads; no `IQueryHandler<TQuery,TResult>`.
13. **`IEventSubscriptionExceptionHandler` loses event context** — handler receives only `Exception`, no stream/event info for dead-lettering.
