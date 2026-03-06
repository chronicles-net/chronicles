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

### 2026-03-05 — Architecture Enforcement with NetArchTest

**Package added:** `NetArchTest.Rules` version 1.3.2 (NOT `NetArchTest.eNET` — that package does not exist on NuGet)

**Test suite:** `test/Chronicles.Tests/Architecture/LayerBoundaryTests.cs` — 6 tests enforcing Lars's architectural directive:
1. `Documents_should_not_reference_EventStore` — base layer cannot reference upper layer
2. `Documents_should_not_reference_Cqrs` — base layer cannot reference upper layer
3. `EventStore_should_not_reference_Cqrs` — middle layer cannot reference upper layer
4. `EventStore_should_not_reference_Documents_Internal` — middle layer uses only PUBLIC types from base (exception: DI wiring in `EventStore.DependencyInjection` is excluded)
5. `Cqrs_should_not_reference_EventStore_Internal` — top layer uses only PUBLIC types from middle
6. `Cqrs_should_not_reference_Documents_Internal` — top layer uses only PUBLIC types from base

**Explicit exception:** `EventStore.DependencyInjection` namespace is permitted to reference `Documents.Internal` for change-feed subscription wiring. This is the ONLY permitted upward dependency across internal boundaries.

**Test outcome (2026-03-05):** All 6 architecture tests pass. No current violations detected. These tests will run in CI and catch any future violations.

### 2026-03-05 — CommandContextExtensionsTests NSubstitute Fix

**Problem:** 4 failing tests in `CommandContextExtensionsTests.cs` at lines 271, 298, 327, 354 — all throwing `CouldNotSetReturnDueToNoLastCallException` from NSubstitute when calling `.Returns()` on `stateContext.GetState<TestState>()`.

**Root cause:** The tests used `[AutoNSubstituteData]` with `IStateContext stateContext` as a parameter, but AutoFixture.AutoNSubstitute was creating a concrete `StateContext` instance instead of a substitute (likely via the `IStateContext.Create()` static factory method). Concrete instances cannot have `.Returns()` configured because there's no substitute call to intercept.

**Solution:** Changed all 4 failing tests to explicitly create a substitute for `IStateContext` using `Substitute.For<IStateContext>()` instead of relying on auto-generation from the test framework. Removed `IStateContext stateContext` from test method parameters and added `var stateContext = Substitute.For<IStateContext>();` as the first line of each test body.

**Tests fixed:**
- `WithStateResponse_Should_Build_State_And_Set_As_Response_On_Completed`
- `WithStateResponse_Should_Use_Existing_State_If_Available`
- `WithStateResponse_With_State_Mapper_Should_Map_State_To_Response`
- `WithStateResponse_With_Context_Mapper_Should_Map_State_To_Response`

**Pattern learned:** When an interface has a static factory method (like `IStateContext.Create()`), AutoFixture may prioritize the factory over creating a substitute. For mockable interfaces, explicitly create substitutes with `Substitute.For<T>()` rather than relying on `[AutoNSubstituteData]` parameter injection.

**Test outcome:** All 19 `CommandContextExtensionsTests` pass, and full test suite passes (213/213 tests).

### 2026-03-05 — Event Evolution Documentation

**Deliverable:** Created `docs/event-evolution.md` — comprehensive guide for event schema evolution in Chronicles, per PRD at `docs/proposals/event-evolution-prd.md`.

**Document structure (implemented):**
1. **Overview** — Why events evolve, Chronicles philosophy (streams never throw)
2. **Pattern 1: Event Rename (Aliases)** — Using `AddEvent<T>(name, aliases)` with examples
3. **Pattern 2: Field Addition** — Backwards-compatible defaults in record fields
4. **Pattern 3: Custom Converter** — `IEventDataConverter` implementation for type changes
5. **Pattern 4: Unknown/Faulted Events** — Graceful handling in projections
6. **Testing** — Unit test examples for converters and mixed-version streams
7. **FAQ** — 10 developer-focused questions and answers
8. **Advanced Topics** — Forward compatibility, multi-system normalization, future enhancements

**Key design decisions captured:**
- Aliases are **read-only** mappings (canonical name used for writing)
- Null converter return signals "doesn't recognize this event" → `UnknownEvent`
- `FaultedEvent` wraps deserialization failures without throwing
- Projections should pattern-match on `UnknownEvent` and `FaultedEvent` and skip gracefully
- Custom converters access raw JSON + metadata via `EventConverterContext`

**API types documented with actual signatures:**
- `AddEvent<TEvent>(string name, params string[] aliases)`
- `IEventDataConverter.Convert(EventConverterContext context) → object?`
- `EventConverterContext` properties: `JsonElement Data`, `EventMetadata Metadata`, `JsonSerializerOptions Options`
- `FaultedEvent` and `UnknownEvent` as stream event data wrappers
- `IStateProjection<TState>` and `IDocumentProjection<TDocument>` pattern-matching examples

**Code examples include:**
- Event rename with single and multiple aliases
- Optional field addition with defaults
- Custom converter for `decimal → Money` type change
- State projection with Unknown/Faulted handling
- Document projection with logging
- Unit tests for converter behavior and mixed-version streams
- Tests for graceful skipping of unknown/faulted events

**External resources linked:**
- Martin Fowler's Event Sourcing article
- Event Store's "Versioning in Event Sourced Systems" blog post

**Notable:** Document explicitly notes deferred features (fluent upcasting, lambda converters, schema versioning) to set correct expectations.

**Publication Outcome (2026-03-06):**
- Document incorporated into Event Evolution PRD and team session
- All 4 patterns implemented by Gurney (multi-name API, custom converters, null handling)
- All 10 code examples verified against actual implementation and test suite
- Code review: APPROVED by Thufir as production-ready documentation
- Status: ✅ Ready for publication to public docs
