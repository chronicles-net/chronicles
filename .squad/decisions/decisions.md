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
