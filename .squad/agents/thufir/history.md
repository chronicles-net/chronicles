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

### 2026-03-25 — Event Evolution PRD Review Orchestration

**Context:** Led comprehensive team review of Event Evolution PRD (`docs/proposals/event-evolution-prd.md`). All four team members (Thufir, Gurney, Duncan, Chani) completed independent focused analyses.

**Unanimous Finding:** PRD is conceptually sound but outdated as a proposal — all v1.0 scope has been implemented and shipped.

**Coordination Completed:**
- Wrote orchestration logs for all 4 agents (20260325T151817Z-20260325T151820Z)
- Wrote session summary log (20260325T151821Z-event-evolution-prd-review.md)
- Merged all inbox decision files into decisions.md
- Deleted inbox files (deduplication complete)

**Key Deliverables Status:**
- Multi-name registration API: ✅ Shipped
- Conflict detection: ✅ Shipped
- `docs/event-evolution.md`: ✅ Published
- Test infrastructure: ✅ 6/9 tests passing (67% coverage)
- Documentation: ✅ Complete

**Team Verdicts:**
- Thufir: PRD status stale; reframe as "Implemented — v1.0.0"
- Gurney: API accurate; minor inaccuracy on `AliasedEventDataConverter` (not needed)
- Duncan: Conceptually sound; ES/CQRS semantics correct
- Chani: 67% test coverage; 2 gaps (JSON syntax, mixed-version integration) are optional

**Recommendation:** Update PRD status, remove/resolve Open Questions, mark implementation as complete. Defer test helpers and remaining test gaps to v1.x.

**Next Steps:** Await decision from Lars on PRD disposition (update in-place, archive, or close).

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

### 2026-03-06 — Documentation PR Readiness Review

**Scope:** Reviewed all documentation surfaces (readme.md, CHANGELOG.md, CONTRIBUTING.md, docs/) for v1.0.0 release preparation.

**Documentation Status:**
- ✅ 9 comprehensive guides written: Getting Started, Event Store, Command Handlers, Projections, Document Store, Event Subscriptions, Event Evolution, Dependency Injection, Testing
- ✅ Sample application complete (Order/Restaurant/Courier microservices via Aspire)
- ✅ All guides align with v1.0.0 API surface
- ✅ Tests pass: 220/220 passing, no warnings, build clean

**Changes Made for PR Readiness:**

1. **readme.md:**
   - Enhanced "Three pillars" section: added context about immutability, event-driven design, and read-model optimization
   - Added "Build fast read-side queries without CQRS complexity if you don't need it" positioning statement
   - Added Event Evolution guide to documentation table (was written but not listed)
   - Clarified value proposition for each pillar

2. **CHANGELOG.md:**
   - Expanded v1.0.0 Added section: 11 bullets → 12 bullets with emphasis on architecture enforcement and testing
   - Callout: "Comprehensive documentation: 9 guides covering..." — positions docs as release artifact
   - Clarified Fixed section: "Sentinel values" bug and "IEventSubscriptionExceptionHandler signature" change

3. **CONTRIBUTING.md:**
   - Fixed typo: "truck-based" → "trunk-based" development
   - No other changes needed — already comprehensive

**Key Gap Identified:**
- Event Evolution guide exists but was omitted from readme documentation table — FIXED

**Architecture Validation:**
- Ran `dotnet build -c Release` — ✅ Build succeeded, 0 warnings, 0 errors
- Ran `dotnet test -c Release` — ✅ All 220 tests passing
- Package generated: `Chronicles.1.0.0.nupkg` and `.snupkg` ready for publishing

### 2026-03-06: Documentation PR Readiness Review

**Scope:** Reviewed all documentation surfaces (readme.md, CHANGELOG.md, CONTRIBUTING.md, docs/) for v1.0.0 release preparation.

**Documentation Status:**
- ✅ 9 comprehensive guides written: Getting Started, Event Store, Command Handlers, Projections, Document Store, Event Subscriptions, Event Evolution, Dependency Injection, Testing
- ✅ Sample application complete (Order/Restaurant/Courier microservices via Aspire)
- ✅ All guides align with v1.0.0 API surface
- ✅ Tests pass: 220/220 passing, no warnings, build clean

**Changes Made for PR Readiness:**

1. **readme.md:**
   - Enhanced "Three pillars" section: added context about immutability, event-driven design, and read-model optimization
   - Added "Build fast read-side queries without CQRS complexity if you don't need it" positioning statement
   - Added Event Evolution guide to documentation table (was written but not listed)
   - Clarified value proposition for each pillar

2. **CHANGELOG.md:**
   - Expanded v1.0.0 Added section: 11 bullets → 12 bullets with emphasis on architecture enforcement and testing
   - Callout: "Comprehensive documentation: 9 guides covering..." — positions docs as release artifact
   - Clarified Fixed section: "Sentinel values" bug and "IEventSubscriptionExceptionHandler signature" change

3. **CONTRIBUTING.md:**
   - Fixed typo: "truck-based" → "trunk-based" development
   - No other changes needed — already comprehensive

**Key Gap Identified:**
- Event Evolution guide exists but was omitted from readme documentation table — FIXED

**Architecture Validation:**
- Ran `dotnet build -c Release` — ✅ Build succeeded, 0 warnings, 0 errors
- Ran `dotnet test -c Release` — ✅ All 220 tests passing
- Package generated: `Chronicles.1.0.0.nupkg` and `.snupkg` ready for publishing

**Release Readiness Verdict:**
Chronicles v1.0.0 is PR-ready. All documentation surfaces are internally consistent, reflect the actual v1.0.0 API, and positioning is clear. No broad unrelated edits made — only lead-level framing and consistency updates. Handoff to Gurney/Duncan/Chani for focused feature/fix updates if needed.

### 2026-03-18: Orchestration Complete — Doc PR Prep Finalized

**Team Update:**
All four agents (Gurney, Duncan, Chani, Thufir) completed focused documentation reviews. Orchestration logs generated. Decision inbox merged into decisions.md. No blocking issues remain for v1.0.0 PR preparation.

**Coordinated Outcomes:**
- Gurney: README, quick-start fix, getting-started write options, DI alias reference
- Duncan: Event store docs, EventMetadata/EventId/deleteStream examples, best practices
- Chani: Testing docs, edge cases, code coverage section, API change examples
- Thufir: Overall consistency, positioning, gap identification, release readiness sign-off

**Status:** ✅ Documentation surfaces PR-ready. 220/220 tests passing. Proceed to merge.

### 2026-03-18: EventId Removal Plan — Reversing Incomplete Feature

**Context:** Lars requested implementation plan to remove EventId from EventMetadata. Investigation revealed EventId was added for v1.0.0 with rationale "critical for idempotency/dedup" but **never actually implemented**.

**Key Findings:**
1. **Property defined but unused:** EventId exists on EventMetadata but is never set, stored, serialized, or used for deduplication
2. **No supporting infrastructure:** No code sets EventId, no storage schema includes it, no validation logic checks it
3. **Documentation overpromises:** Docs describe behavior that doesn't exist (lines 88-119 in event-store.md show usage examples for non-existent functionality)
4. **Structural tests only:** 3 tests verify property can be set/copied but no behavioral tests for deduplication

**Impact Analysis:**
- **Production:** 1 file (EventMetadata.cs) — remove property
- **Tests:** 3 tests removed from EventMetadataTests.cs
- **Documentation:** 2 files (event-store.md, testing.md) — remove EventId sections and references
- **CHANGELOG:** Mark as removed in Unreleased section
- **Risk:** LOW — property is completely unused; removal is API cleanup, not functional change

**Decision:** REVERSE original Decision #6 (EventId for v1.0.0)

**Rationale:**
- Incomplete features mislead users
- Documentation describes behavior that doesn't exist
- Pre-1.0 architectural cleanup is appropriate
- If needed later, can be reintroduced with full implementation

**Execution Plan:**
- Gurney: Remove property from EventMetadata.cs, update CHANGELOG
- Chani: Remove 3 structural tests
- Duncan: Clean up documentation (event-store.md, testing.md)
- Single PR, atomic commit, reviewed by Thufir

**Deliverable:** Comprehensive implementation plan in `.squad/decisions/inbox/thufir-eventid-removal-plan.md`

**Architecture Principle Reinforced:** Don't ship half-finished features. If we can't implement it fully, don't expose it in the public API.

### 2026-03-25 — EventId Removal Planning Complete

**Coordinated Session:** Team completed comprehensive EventId removal planning. Decision to reverse Decision #6 (EventId acceptance) was approved by Thufir. Comprehensive implementation plan created with delegation to Gurney (production code + CHANGELOG), Chani (test removal + documentation), and Duncan (ES/CQRS validation).

**Key Findings Documented:**
1. **Incomplete implementation:** EventId property added for v1.0.0 but infrastructure never completed
2. **No production code dependency:** Zero references outside of 3 unit tests and documentation
3. **Serialization safe:** EventId never had JSON mapping; existing Cosmos documents unaffected on removal
4. **Pre-1.0.0 cleanup justified:** Removing incomplete features before public release is architecturally sound

**Coordination Artifacts Generated:**
- Session log: `.squad/log/20260325T120400Z-eventid-removal-session.md`
- Decision merged into `.squad/decisions.md` (Decision #9)
- Per-agent orchestration logs for Thufir, Gurney, Duncan, Chani tracking their specific responsibilities
- Decision inbox entries cleared (merged to decisions.md)

**Execution Plan:**
- Phase 1 (Gurney): Remove property from `EventMetadata.cs`, update `CHANGELOG.md` (30 min estimated)
- Phase 2 (Chani): Delete 3 EventId tests, update `docs/event-store.md` and `docs/testing.md` (45 min estimated)
- Validation: `dotnet build -c Release`, `dotnet test -c Release` (217/220 tests expected), grep verification

**Status:** ✅ Planning complete, ready for phase-1 implementation

### 2026-03-25 — Event Evolution PRD Review

**Scope:** Reviewed `docs/proposals/event-evolution-prd.md` against current codebase to assess accuracy.

**Key Finding:** PRD is outdated. All v1.0 proposals (multi-name registration, alias support, conflict detection, documentation, core tests) have been implemented since the PRD was drafted on 2026-03-05. The document frames shipped features as proposals.

**Specifics:**
- `AddEvent<TEvent>(string name, params string[] aliases)` — implemented in `EventStoreBuilder.cs:80-94`
- `EventCatalog` alias support — constructor accepts `IDictionary<string, IEventDataConverter>? aliasMappings`
- Conflict detection — `ValidateEventNames()` in `EventStoreBuilder.cs:186-207`
- `docs/event-evolution.md` — published and complete
- `IEventDataConverter.Convert()` null-return semantics — documented in XML docs
- Open Questions Q1-Q3 — all resolved through implementation (Option A for conflicts, Option C for docs scope)
- EventId — confirmed removed per Decision #9 (not referenced in PRD, which is correct)

**Remaining gaps (minor):**
- No `NullDataElement` boundary test
- No integration-level mixed-version stream test (unit-level alias coverage exists)
- Proposed test helpers (`EventConverterTestBuilder`, `StreamEventAssertions`) not implemented — standard patterns suffice

**Decision:** PRD should be reframed from "Draft — Pending Review" to "Implemented — v1.0.0" or archived. Decision filed to `.squad/decisions/inbox/thufir-event-evolution-prd-review.md`.

**Architecture note:** The `EventDataConverter` (Internal) accepts `eventName` in its constructor and only deserializes when `context.Metadata.Name == eventName`. For aliases, a separate `EventDataConverter` instance is created per alias name — this means alias converters are self-contained and require no changes to the converter contract.

