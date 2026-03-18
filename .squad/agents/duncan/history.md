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

### 2026-03-25: Event Evolution PRD Review — ES/CQRS Domain Analysis

**Task:** Comprehensive ES/CQRS-domain review of Event Evolution PRD (`docs/proposals/event-evolution-prd.md`) to validate conceptual soundness and identify staleness.

**Work Completed:**
- Reviewed PRD problem statement, architecture diagrams, evolution patterns, test infrastructure, documentation
- Validated against current codebase implementation
- Assessed domain correctness from ES/CQRS perspective

**Conceptual Soundness Assessment:**

**✅ Sections Requiring No Changes:**
1. Problem Statement (Section 1) — accurately captures ES evolution friction
2. Current Architecture (Section 2) — pipeline diagram and types match implementation
3. Evolution Scenarios (Section 3) — all four patterns are canonical ES/CQRS practices
4. Success Criteria (Section 8) — all v1.0 release criteria met

**⚠️ Sections Requiring Updates:**
1. Document Status: Change from "Draft — Pending Maintainer Review" to "Implemented — v1.0.0"
2. Open Questions (Section 6): All three resolved; add resolution block or remove
3. Section 5 Headings: Change from "Proposed" to past-tense

**Domain Correctness: ✅ SOUND**

**Event Sourcing Semantics:**
- Immutability preserved (aliases are read-only, historical events never mutated)
- Stream integrity maintained (UnknownEvent/FaultedEvent wrappers prevent stream read failures)
- Versioning philosophy correct (optional, user-driven evolution without forced migrations)
- Backwards compatibility sound (old names coexist with new without data rewrite)

**CQRS Alignment: ✅ CORRECT**
- Write-side isolation: Primary name used for writes (consistency)
- Read-side tolerance: Both primary and aliases accepted (resilience)
- Projection responsibility: Projections decide handling of unknown/faulted events (separation of concerns)

**Concurrency & Consistency: ✅ NO ISSUES**
- Alias registration at build-time (no runtime race conditions)
- Validation at DI registration (fail-fast before first event read)

**Key Finding:** PRD is a high-quality planning artifact that correctly predicted the implementation. No conceptual flaws. Aliases are the correct pattern for event renames. Implementation matches domain semantics perfectly.

**Recommendation:** Update PRD to serve as historical design record rather than active proposal. Alternatively, archive to `docs/archive/proposals/` with completion summary.

**ES/CQRS Verdict:** No concerns. Implementation is correct. Ship it.

### 2026-03-18: Documentation PR Prep Orchestration — Finalized

**Coordinated Session:** All four agents completed focused documentation reviews. Orchestration logs generated, decision inbox merged, agent histories synchronized.

**Team Contributions:**
- Thufir (Lead): Overall consistency, positioning clarity, gap identification (Event Evolution in readme)
- Gurney (Backend): Quick-start fix, write options section, DI alias docs
- Duncan (ES/CQRS): Event store docs (EventMetadata, EventId, deleteStream), best practices
- Chani (Tester): Testing docs (edge cases, coverage, API changes)

**Outcome:** All documentation surfaces aligned and PR-ready. 220/220 tests passing.

### 2026-03-06: Documentation PR Prep Audit

**Mission:** Ensure ES/CQRS docs are ready for v1.0.0 release to main branch.

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

### 2026-03-06 — Documentation PR Prep Audit

**Mission:** Ensure ES/CQRS docs are ready for v1.0.0 release to main branch.

**Audit findings:**
- ✅ CHANGELOG.md — Already comprehensive, captures all v1.0.0 features (EventId, expectedVersion, CloseAsync, IEventSubscriptionExceptionHandler)
- ✅ event-subscriptions.md — Already documents updated IEventSubscriptionExceptionHandler signature correctly
- ✅ All other core docs (command-handlers.md, projections.md, event-evolution.md, getting-started.md, readme.md) — Accurate, no changes needed

**Gaps identified & fixed in event-store.md:**
1. **EventMetadata.EventId** — Added new subsection documenting all 6 metadata fields (Name, Timestamp, Version, StreamId, CorrelationId, CausationId, EventId) with code examples and "Using EventId for Idempotency" subsection showing Guid-based deduplication pattern
2. **DeleteStreamAsync.expectedVersion** — Enhanced "Close and Delete" section with example showing safe concurrent deletion with expectedVersion guard and StreamConflictException handling
3. **Best Practices** — Added 3 new entries: use EventId for idempotent operations, set expectedVersion on delete, prefer CloseAsync over deletion

**Verification:**
- Build: 0 warnings, 0 errors
- Tests: 220/220 pass
- No regressions

**Outcome:** Documentation is PR-ready. All v1.0.0 features documented with clear examples and guidance.

**Decision document:** .squad/decisions/inbox/duncan-doc-pr-prep.md

### 2026-03-07 — EventId Removal Analysis

**Mission:** Analyze feasibility and implications of removing EventId from EventMetadata.

**Analysis scope:**
1. Codebase usage patterns for EventId
2. Documentation/decision record accuracy post-removal
3. Backward compatibility implications
4. Implementation plan & user clarifications needed

**Key findings:**

- **EventId is completely unused internally** — Never populated by `EventDocumentBatchProducer.Convert()`, never accessed by any command handler or projection, only mentioned in unit tests and documentation
- **Design intent was user-supplied deduplication** — EventId was documented as optional, user-populated for idempotency. Chronicles does NOT manage it
- **Three minimal unit tests exist** — Only verifying that the property exists and can be mutated via record `with` syntax; no integration tests
- **Zero usage in samples** — The sample Courier application never sets EventId
- **Backward compat is sound** — Existing Cosmos DB documents with `eventId` JSON fields will deserialize successfully even after removal (unknown JSON fields ignored)

**Removal is justified because:**
1. No framework value — Event ID management is entirely user-responsibility
2. Adds API surface clutter — Unused optional property complicates the mental model
3. Pre-1.0.0 timing — Acceptable breaking change before release tag
4. Low implementation cost — Delete 1 property, 3 tests, update 3 doc files

**Documentation updates required:**
- `docs/event-store.md` — Remove EventId field documentation, idempotency pattern example, best practice
- `docs/testing.md` — Remove EventId section and idempotency verification guidance
- `CHANGELOG.md` — Remove EventId from features list, optionally add to Removed section with rationale

**Implementation blocker: TIMING** — Removal must occur before v1.0.0 release tag. After release, removing EventId is a breaking change requiring v2.0.0.

**User clarification needed:**
1. Should removal be in v1.0.0 final or deferred to v1.1?
2. Should CHANGELOG explain removal rationale (e.g., "unused internally; idempotency better handled at application level")?
3. Should documentation include alternative idempotency patterns (external dedup table, command result cache)?

**Decision document:** .squad/decisions/inbox/duncan-eventid-removal-analysis.md

### 2026-03-25 — EventId Removal Validation & Coordination Owner

**Task:** ES/CQRS expert owner for EventId removal validation and documentation coordination.

**Coordination Responsibilities:**
1. **Validation:** Confirm EventId removal is technically sound (serialization impact, backward compatibility)
2. **Documentation Coordination:** Ensure Gurney/Chani changes maintain consistency and completeness
3. **Final Review:** Pre-merge sign-off on removal scope and validation

**Analysis Findings (Summary):**
- **No production code dependency:** Zero references in command handlers, projections, or event processors
- **Never populated:** EventDocumentBatchProducer never sets EventId during event persistence
- **Never serialized:** No JSON mapping; never persists to Cosmos DB
- **Serialization safe:** Existing Cosmos documents with `eventId` fields will deserialize correctly after removal (unknown JSON fields ignored)
- **Documentation currently misleading:** event-store.md and testing.md describe behavior that doesn't exist

**Risk Assessment:** ✅ LOW
- Removal is transparent to framework logic
- No migration path needed (property never written)
- Breaking change only affects code that directly accessed EventMetadata.EventId (only test code)

**Validation Coordination for Team:**

**For Gurney:**
- Production code change straightforward (property removal only)
- No serialization logic impact
- CHANGELOG update should note: "Removed unused EventId property; infrastructure never implemented"

**For Chani:**
- 3 tests purely structural (safe to delete)
- 9 indirect tests verify EventMetadata core functionality (unaffected)
- Documentation removal is comprehensive (all EventId references targeted)

**For Thufir (Audit):**
- No architectural violations (removal doesn't affect layer boundaries)
- API surface improvement (eliminates dead code)
- Pre-1.0.0 timing acceptable

**Validation Checklist:**
- [ ] Confirm no EventId references in production code (post-removal)
- [ ] Verify test count: 220 → 217 (3 deleted)
- [ ] Ensure documentation consistency (no dangling references)
- [ ] Build green: `dotnet build -c Release` (0 warnings)
- [ ] Tests pass: `dotnet test -c Release` (217/217)

**Status:** ✅ Analysis complete. Ready for team implementation with Duncan providing validation coordination during phases 1-2.

**Learning:** Pre-release API cleanup (removing incomplete/unused features) is far preferable to shipping half-finished features. Customers reading docs expect EventId to work but it doesn't — removal eliminates user confusion. Decision #6 (acceptance of EventId) is reversed due to incomplete implementation post-acceptance.

### 2026-03-25 — Event Evolution PRD Review

**Mission:** Review `docs/proposals/event-evolution-prd.md` for accuracy against current codebase state and identify needed adjustments.

**Context:** PRD authored 2026-03-05 as planning document for v1.0 event evolution features (multi-name registration API, test infrastructure, documentation).

**Key Findings:**

1. **PRD is conceptually sound** — Problem analysis, architecture diagrams, evolution patterns (rename, field addition, type change, unknown/faulted handling) are correct ES/CQRS practices
2. **v1.0 scope FULLY IMPLEMENTED** — All proposed features shipped:
   - Multi-name API: `EventStoreBuilder.AddEvent<T>(name, aliases)` at lines 80-94
   - Alias semantics: Read-only mappings, primary name for writing (correct)
   - Conflict validation: `ValidateEventNames()` throws `InvalidOperationException` (lines 186-207)
   - Test coverage: All 6 required edge-case tests implemented and passing
   - Documentation: `docs/event-evolution.md` published (per 2026-03-05 history entry)
3. **Document status STALE** — Says "Draft — Pending Maintainer Review" but reality is "Shipped in v1.0.0"
4. **Open questions RESOLVED** — All three maintainer questions answered during implementation:
   - Q1 (Null return): Confirmed as intentional opt-out → `UnknownEvent` (XML docs added)
   - Q2 (Alias conflicts): Option A (fail-fast) implemented with clear error messages
   - Q3 (Documentation scope): Option C (Chronicles-specific) implemented with external links

**Domain Correctness Assessment:**
- ✅ Event sourcing semantics: Immutability preserved, stream integrity maintained via wrappers
- ✅ CQRS alignment: Write-side uses primary name (consistency), read-side accepts aliases (resilience)
- ✅ No concurrency issues: Alias registration is build-time, validation at DI registration

**Recommended Adjustments:**
1. **Update status** from "Draft" to "Implemented — v1.0.0"
2. **Close open questions** with resolution notes and implementation references
3. **Change section headings** from "Proposed" to past-tense ("Implemented", "Shipped API")
4. **Add implementation references** to Section 5 (builder, catalog, converter, test files)
5. **Mark success criteria** as achieved with timestamps

**Alternative:** Archive PRD to `docs/archive/proposals/` and create `docs/event-evolution-design.md` as canonical design record reflecting shipped state.

**Semantic Analysis:** Aliases are the CORRECT pattern for event renames in ES systems. Implementation correctly preserves stream immutability while providing read-side flexibility. No domain flaws identified.

**Decision document:** `.squad/decisions/inbox/duncan-event-evolution-prd-review.md`

**Pattern learned:** PRDs that accurately predict implementation become valuable historical design records. Keep them updated to serve as "why we built it this way" documentation for future maintainers.
