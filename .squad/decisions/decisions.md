# Decisions Log

## 2026-03-05: Architectural Layer Enforcement Directive

**Date:** 2026-03-05T06:46:09Z  
**By:** Lars Skovslund (via Copilot)  
**Status:** Adopted

### Decision

The architectural layering must always be enforced:

1. **Documents** — lowest level, the foundation
2. **EventStore** — built on top of Documents; may only use public interfaces and classes from Documents
3. **CQRS** — built on top of EventStore; may only use public interfaces and types from EventStore and Documents

Any violation (a higher layer referencing internal/non-public types from a lower layer, or a layer skipping a layer) is forbidden unless explicitly documented as an exception.

### Rationale

User request — captured for team memory and enforcement in all code review and development decisions.
# Decisions Log — Chronicles

## 2026-03-06: Multi-Name Event Registration API Design

**Author:** Gurney (Backend Dev)  
**Date:** 2026-03-06  
**Status:** ✅ IMPLEMENTED & APPROVED  
**PR:** #27 (feature/multi-name-event-registration)

### Summary
Implemented `AddEvent<TEvent>(string name, params string[] aliases)` overload enabling backwards-compatible event renames without custom `IEventDataConverter` implementations.

### Design Decisions

#### 1. No new converter class
Created separate `EventDataConverter` instances for each alias rather than a new `AliasedEventDataConverter`. Each converter matches on its own name via `context.Metadata.Name == eventName`. Simpler, no new internal types, same behavior.

#### 2. Conflict detection in `Build()`, not at registration time
Validates all names (primary + aliases) are unique after all registrations are complete. Catches: duplicate primaries, alias-vs-primary conflicts, alias-vs-alias conflicts. Error message: `"Event name '{name}' is already registered."`

#### 3. `params string[]` overload — no ambiguity
C# overload resolution cleanly separates:
- `AddEvent<T>("name")` → first overload (non-params preferred)
- `AddEvent<T>("name", converter)` → IEventDataConverter overload
- `AddEvent<T>("name", "alias1", "alias2")` → params overload

#### 4. EventCatalog backwards-compatible constructor
Added optional `IDictionary<string, IEventDataConverter>? aliasMappings = null` parameter. Existing callers (including testing fakes) unaffected.

### Files Changed
| File | Change |
|------|--------|
| `EventStoreBuilder.cs` | New overload, alias tracking, `ValidateEventNames()` |
| `EventCatalog.cs` | Optional alias mappings in constructor |
| `IEventDataConverter.cs` | XML docs: null return → UnknownEvent |

### Test Coverage
- ✅ EventStoreBuilderTests (3 conflict detection tests)
- ✅ Converter null-return edge case test
- ✅ 5 alias registration and retrieval tests
- ✅ All 220 tests passing

### Approval
**Thufir (Lead):** APPROVED — production-ready implementation.

---

## 2026-03-06: Event Evolution Test Coverage Strategy

**Author:** Chani (Tester)  
**Date:** 2026-03-06  
**Status:** ✅ IMPLEMENTED & VERIFIED

### Summary
Event evolution test gaps identified and addressed:
1. Converter null-return behavior → added test verifying `UnknownEvent` production
2. Alias registration scenarios → 5 tests staged and passing
3. All existing 214 tests continue to pass

### Tests Added
**StreamEventConverterTests.cs:**
- `Convert_Should_Return_UnknownEvent_When_Converter_Returns_Null` ✅

**EventCatalogTests.cs (5 alias tests):**
- `GetConverter_Should_Return_Converter_For_Alias`
- `GetConverter_Should_Return_Same_Converter_For_Primary_And_Alias`
- `GetEventName_Should_Return_Primary_Name_Even_When_Aliases_Exist`
- `Constructor_Should_Throw_On_Duplicate_Alias`
- `Constructor_Should_Throw_On_Alias_Conflicting_With_Primary_Name`

### Design Assumptions (verified)
- EventCatalog accepts `IDictionary<string, IEventDataConverter>?` as second constructor parameter
- Aliases map to same converter as primary name
- `GetEventName(Type)` returns canonical name only
- Duplicate/conflicting aliases throw `InvalidOperationException`

### Test Outcome
All 220 tests passing. No regressions.

---

## 2026-03-06: Documentation PR Preparation — All Surfaces

**Scope:** Comprehensive review and validation of all documentation for v1.0.0 PR readiness

### By Thufir (Lead) — PR Readiness & Consistency Review

**Changes Made:**
1. **readme.md** — Enhanced three pillars positioning with clarity on immutability, event-driven design, and read-model optimization. Added Event Evolution guide to documentation table (was written but missing from index).
2. **CHANGELOG.md** — Expanded v1.0.0 Added section from 8 to 12 bullets: called out Event Evolution, architecture enforcement (NetArchTest), and testing infrastructure (AddFakeChronicles).
3. **CONTRIBUTING.md** — Fixed typo: "truck-based" → "trunk-based" development.

**Key Finding:** Event Evolution guide existed but was omitted from readme documentation table — FIXED.

**Verdict:** ✅ Chronicles v1.0.0 is PR-ready. All documentation surfaces align with API, codebase state, and release narrative.

### By Gurney (Backend Dev) — README & Onboarding

**Changes Made:**
1. **readme.md** — Fixed quick-start bug: `evt.EventType` (non-existent) → `evt.Metadata.Name` (correct API)
2. **docs/getting-started.md** — Added "Write Options" section documenting `StreamWriteOptions` for correlation/causation IDs
3. **docs/dependency-injection.md** — Added "Event Aliases" subsection with quick-reference to Event Evolution guide
4. **CHANGELOG.md** — Updated v1.0.0 release date to 2026-03-06

**Key Insight:** Quick-start code bug caught — would prevent users from running sample code. Documentation audit catches real API mismatches.

**Status:** ✅ All updates verified, 220/220 tests passing.

### By Duncan (ES/CQRS Expert) — Event Store & CQRS Docs

**Gaps Identified & Fixed in docs/event-store.md:**
1. **EventMetadata.EventId** — Added new subsection documenting all 6 metadata fields with code example and "Using EventId for Idempotency" subsection showing Guid-based deduplication pattern
2. **DeleteStreamAsync.expectedVersion** — Enhanced "Close and Delete" section with safe concurrent deletion example and StreamConflictException handling
3. **Best Practices** — Added 3 new entries: use EventId for idempotent operations, set expectedVersion on delete, prefer CloseAsync over deletion

**Files Verified (No Changes):** CHANGELOG.md, event-subscriptions.md, command-handlers.md, projections.md, event-evolution.md, getting-started.md

**Status:** ✅ Documentation PR-ready. All v1.0.0 features documented with clear examples.

### By Chani (Tester) — Testing Documentation

**Changes Made to docs/testing.md:**
1. Added framework context (xUnit v3, FluentAssertions, AutoFixture standards)
2. Added "Delete with Version Safety" example with expectedVersion parameter
3. Expanded "Projections" section with state rebuilding verification guidance
4. Added "API Changes in v1.0.0" section (EventId, CloseAsync, IEventSubscriptionExceptionHandler signature)
5. Added "Testing Edge Cases" section (empty streams, sentinel values)
6. Added "Code Coverage" section (XPlat Coverage, badge auto-commit expectations)

**Changes to readme.md:** Updated testing guide link description for better discoverability

**Quality Assessment:** ✅ No outdated API references, examples match conventions, edge cases documented, coverage expectations clear.

**Test Results:** 220/220 passing, 0 failures, 0 warnings.

### Overall Status: ✅ ALL SURFACES PR-READY

- 220/220 tests passing, 0 warnings
- All 9 documentation guides complete and accurate
- API examples verified against implementation
- Architecture decisions (EventId, expectedVersion, CloseAsync, aliases) documented with examples
- Testing infrastructure and edge cases documented
- Code coverage integration documented
- No blocking issues for v1.0.0 release

---

## 2026-03-18: User Directive — Code Style

**By:** Lars Skovslund (via Copilot)  
**Date:** 2026-03-18T13:36:34Z  
**Status:** Directive Captured

### Directive
Do not use underscore-prefixed members in code or example code.

### Context
User request — captured for team memory and enforcement in all code generation and documentation decisions.

---

## 2026-03-05: Architecture Enforcement with NetArchTest

**Author:** Duncan (ES/CQRS Expert)  
**Date:** 2026-03-05  
**Status:** ✅ IMPLEMENTED & PASSING

### Summary
Implemented `LayerBoundaryTests.cs` to enforce Lars's architectural directive: strict layering (Documents → EventStore → CQRS, no upward references except DI wiring).

### Tests Implemented (6 total)
1. `Documents_should_not_reference_EventStore` ✅
2. `Documents_should_not_reference_Cqrs` ✅
3. `EventStore_should_not_reference_Cqrs` ✅
4. `EventStore_should_not_reference_Documents_Internal` ✅
5. `Cqrs_should_not_reference_EventStore_Internal` ✅
6. `Cqrs_should_not_reference_Documents_Internal` ✅

### Permitted Exception
`EventStore.DependencyInjection` namespace may reference `Documents.Internal` for change-feed subscription wiring. This is the ONLY permitted upward dependency across internal boundaries.

### Test Outcome
All 6 architecture tests passing. No violations detected.

---

## 2026-03-05: CommandContextExtensionsTests NSubstitute Fix

**Author:** Duncan (ES/CQRS Expert)  
**Date:** 2026-03-05  
**Status:** ✅ FIXED & VERIFIED

### Problem
4 failing tests in `CommandContextExtensionsTests.cs` throwing `CouldNotSetReturnDueToNoLastCallException` from NSubstitute when calling `.Returns()` on `stateContext.GetState<TestState>()`.

### Root Cause
`[AutoNSubstituteData]` was creating concrete `StateContext` instances (via static factory method) instead of substitutes. Concrete instances cannot have `.Returns()` configured.

### Solution
Changed all 4 failing tests to explicitly create substitutes with `Substitute.For<IStateContext>()`:
- `WithStateResponse_Should_Build_State_And_Set_As_Response_On_Completed`
- `WithStateResponse_Should_Use_Existing_State_If_Available`
- `WithStateResponse_With_State_Mapper_Should_Map_State_To_Response`
- `WithStateResponse_With_Context_Mapper_Should_Map_State_To_Response`

### Test Outcome
All 19 CommandContextExtensionsTests passing. Full test suite: 213/213.

---

## 2026-03-05: Event Evolution PRD Draft

**Author:** Duncan (ES/CQRS Expert)  
**Date:** 2026-03-05  
**Status:** ✅ DRAFTED & APPROVED

### Summary
Comprehensive event evolution reference guide (`docs/event-evolution.md`) covering 4 evolution patterns, test examples, FAQ, and advanced topics.

### Patterns Covered
1. **Event Rename (Aliases)** — Using `AddEvent<T>(name, aliases)`
2. **Field Addition** — Backwards-compatible defaults in record fields
3. **Custom Converter** — `IEventDataConverter` implementation for type changes
4. **Unknown/Faulted Events** — Graceful handling in projections

### Document Stats
- 442 lines
- 7 sections (Overview, 4 patterns, Testing, FAQ, Advanced)
- 10 code examples
- 10 FAQ entries
- External references (Martin Fowler, Event Store blog)

### Design Decisions Captured
- Aliases are **read-only** mappings (canonical name used for writing)
- Null converter return signals `UnknownEvent`
- `FaultedEvent` wraps deserialization failures
- Projections should pattern-match and skip gracefully
- Custom converters access raw JSON + metadata via `EventConverterContext`

### Approval
Incorporated into Event Evolution PRD and approved by team.

---

## 2026-03-25: Event Evolution PRD Review — Status Update

**Date:** 2026-03-25  
**Reviewer:** Thufir (Lead)  
**Trigger:** PRD review requested by Lars Skovslund  
**Status:** ✅ COMPLETE — All v1.0 scope implemented

### Finding

The PRD (`docs/proposals/event-evolution-prd.md`) was written 2026-03-05 as a forward-looking proposal targeting "Chronicles v1.0." Since then, **all three v1.0 deliverables have been implemented and shipped:**

| PRD Proposal | Current Status |
|---|---|
| Multi-name registration API (`AddEvent<T>(name, aliases)`) | ✅ Shipped — `EventStoreBuilder.cs` lines 80-94 |
| Alias support in `EventCatalog` | ✅ Shipped — constructor accepts alias mappings |
| Conflict detection (`ValidateEventNames()`) | ✅ Shipped — throws `InvalidOperationException` |
| `docs/event-evolution.md` guide | ✅ Published — comprehensive guide with 4 patterns |
| `IEventDataConverter` null-return XML docs | ✅ Documented — lines 12-17 |
| Converter-returns-null test | ✅ Exists — `StreamEventConverterTests` line 128 |
| Alias conflict tests | ✅ Exist — `EventStoreBuilderTests` (3 tests) |
| EventCatalog alias tests | ✅ Exist — `EventCatalogTests` (3+ alias tests) |

The PRD's "Open Questions" (Q1-Q3) have all been resolved through implementation.

### Team Verdicts

**Thufir (Lead):** PRD is outdated; recommend reframing as "Implemented — v1.0.0" or archiving. All v1.0 deliverables complete.

**Gurney (Backend Dev):** API implementation 100% accurate. Test infrastructure complete. Docs shipped. Minor inaccuracy: PRD refers to non-existent `AliasedEventDataConverter` — implementation uses default `EventDataConverter` per alias (simpler).

**Duncan (ES/CQRS Expert):** PRD is conceptually sound and architecturally correct. No domain flaws. Recommend moving from "Draft" to "Implemented — v1.0.0" and marking open questions as "Resolved."

**Chani (Tester):** 67% of test deliverables implemented (6 of 9 tests done). Two gaps remain: JSON syntax error test and mixed-version stream integration test. These are nice-to-haves, not blockers. Test helpers deferred.

### Recommendation

1. Update PRD status from "Draft — Pending Maintainer Review" to "Implemented — v1.0.0"
2. Remove or resolve Open Questions section (all answered by code)
3. Remove Implementation Checklist checkboxes (all checked)
4. Keep Deferred Features section as-is (still valid roadmap)
5. Remove proposed `EventConverterTestBuilder` / `StreamEventAssertions` test helpers — standard xUnit+FluentAssertions patterns suffice

### Remaining Test Gaps (Low-Priority)

Two tests remain unwritten but are non-blocking:
- `MalformedJson_ProducesFaultedEvent` — boundary condition (JSON syntax error, not type mismatch)
- `MixedVersionStream_DeserializesAll` — integration-level test (unit-level alias coverage exists)

### Next Steps

Await decision from Lars: Keep PRD as-is with "✅ SHIPPED" banner, update technical details, or archive to `docs/archive/proposals/`?
