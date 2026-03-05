# Squad Decisions

## Architectural Layer Enforcement

**Date:** 2026-03-05  
**Leads:** Thufir (Audit), Gurney (Cross-Layer Audit), Lars Skovslund (Directive)

### Directive

Documents (lowest) → EventStore (middle) → CQRS (highest). Layers must respect dependency boundaries:
- EventStore may use **public** Documents types only
- CQRS may use **public** EventStore and Documents types only
- Internal namespaces are assembly-wide in single-assembly structure — no compiler enforcement without project split

### Current Violations (2 Critical, 1 Potential)

#### Violation 1: EventStore → Documents.Internal ⛔

**File:** `src/Chronicles/EventStore/DependencyInjection/EventStoreBuilder.cs`

**Issue:** Directly instantiates internal Documents types for change-feed subscriptions:
- `IChangeFeedFactory` (internal interface)
- `DocumentSubscription<TDocument, TProcessor>` (internal class)

**Context:** Lines 114-123 wire event subscriptions via DI. No public Documents API exists for this use case.

#### Violation 2: Cqrs → EventStore.Internal ⛔

**Files:**
- `src/Chronicles/Cqrs/Internal/CommandProcessor.cs` (line 2)
- `src/Chronicles/Cqrs/Internal/DocumentProjectionRebuilder.cs` (line 4)

**Issue:** Both directly instantiate `EventStore.Internal.StateContext` (lines 61, 83, and 22 respectively). No public factory method exists.

**Root Cause:** No `IStateContext` public factory or static creation method.

#### Violation 3: Public API Leaking Internal Types ⚠️

**File:** `src/Chronicles/Documents/InitializationOptions.cs`

**Issue:** Public methods instantiate internal initializers:
- `SubscriptionInitializer` (internal)
- `DocumentInitializer` (internal)

**Status:** Needs investigation — may be intended design for builder pattern.

### Enforcement Options

#### Option A: Separate Projects (Strongest) ⭐

Split into `Chronicles.Documents`, `Chronicles.EventStore`, `Chronicles.Cqrs` with explicit `ProjectReference` chains.

**Pros:**
- `internal` becomes layer-scoped — violations become **compile errors**
- Zero runtime cost; zero maintenance overhead
- Permanent, enforceable solution

**Cons:**
- One-time refactoring (move files, update namespaces)
- Multi-package publishing complexity
- Potential breaking changes (namespace mutations)

**Verdict:** Ideal for pre-1.0 project. Right time to invest.

#### Option B: Architecture Tests (Medium)

Add `NetArchTest` or custom Roslyn analyzer to validate layer boundaries at CI time.

**Pros:**
- No structural changes
- Catches violations in CI

**Cons:**
- Still allows compile; fails at test time
- Requires ongoing maintenance
- Can be disabled

**Verdict:** Good interim measure if project split deferred.

#### Option C: Convention + Code Review (Weakest)

Rely on team discipline.

**Verdict:** Already insufficient — violations exist today.

### Recommendations

**Primary:** Option A (separate projects) — schedule refactoring before 1.0.

**Immediate fixes** (independent of project split):
1. Add public factory method to `StateContext` or expose via `IStateContext` interface
2. Promote Documents change-feed API to public or redesign EventStore subscription registration pattern

**Interim:** Option B — implement architecture tests immediately to prevent further violations.

### Testing Exception

The `Testing/` folder references `Documents.Internal` and `EventStore.Internal` types to provide test fakes. This is **architecturally acceptable** — test-support layers require internal access by design. Document as an explicit exception.

### Summary Table

| Pair | Status | Violations |
|------|--------|------------|
| EventStore → Documents (public) | ✅ | None |
| EventStore → Documents.Internal | ⛔ | EventStoreBuilder.cs uses 3 internal types |
| Cqrs → EventStore (public) | ✅ | None |
| Cqrs → EventStore.Internal | ⛔ | CommandProcessor.cs + DocumentProjectionRebuilder.cs use StateContext |
| Cqrs → Documents (public) | ✅ | Permitted |
| Testing → Documents.Internal | ⚠️ Exception | Test fakes — allowed |

---

## Active Decisions

### 1. EventStore Design Review — Duncan (2026-03-04)

**Context:** Comprehensive review of `src/Chronicles/EventStore/` and `src/Chronicles/Cqrs/` identified 2 critical bugs and 10 design concerns requiring team decision.

**Critical Bugs:**
- **AddEventWhen double-emit** (`CommandContextExtensions.cs`): Event factory called twice in `respondWith` overloads, writing duplicate events per command. **Assigned:** Gurney (fix), Chani (regression test).
- **EnsureSuccess version check** (`StreamMetadataExtensions.cs`): Raw equality check conflicts with `StreamVersion.Any` sentinel, incorrectly rejecting non-empty streams. **Action:** Gurney to evaluate sentinel redesign (Options A/B/C).

**Design Concerns Requiring Decision:**
1. Add `string? EventId` to `EventMetadata` for idempotency and deduplication in at-least-once delivery scenarios
2. Add `int? SchemaVersion` to `EventMetadata` for explicit event upcasting support
3. Add `DateTimeOffset CreatedAt` to `StreamMetadata` for creation date queries
4. Add `StreamVersion? expectedVersion` or `StreamState? expectedState` guard to `DeleteStreamAsync`
5. Rename `ICommandHandler<TCommand>` to `IStreamCommandHandler<TCommand>` to clarify it is stateful (not stateless)
6. Remove `TCommand` parameter from `ConsumeEvent` signature or justify its presence
7. Decide: Implement `CloseAsync` and `Archived` state or remove from public API as incomplete stubs
8. Add `IQueryHandler<TQuery, TResult>` abstraction for read-side queries or document direct document store access as intended pattern
9. Extend `IEventSubscriptionExceptionHandler.HandleAsync(Exception)` to include `StreamEvent?` and `CancellationToken` for dead-letter analysis
10. Document implicit read-side architecture: direct Cosmos document access vs explicit query modeling

**Source:** `.squad/decisions/inbox/duncan-eventstore-review.md` (merged 2026-03-04T15:41)

### 2. StateContext Public Factory — Gurney (2026-03-05)

**Status:** ✅ Implemented

**Problem:** CQRS code violated architectural principle by directly referencing `Chronicles.EventStore.Internal.StateContext`:
- `CommandProcessor.cs` — used `new StateContext()` and typed parameters as `StateContext`
- `DocumentProjectionRebuilder.cs` — used `new StateContext()` for projection rebuilds

**Solution:** Added public factory method to `IStateContext` interface:
```csharp
public interface IStateContext
{
    static IStateContext Create() => new Internal.StateContext();
    // ... existing members
}
```

Updated consuming files:
- Removed `using Chronicles.EventStore.Internal;`
- Replaced `new StateContext()` → `IStateContext.Create()`
- Changed parameter types from `StateContext` → `IStateContext`

**Architectural Exceptions Documented:**
1. **EventStoreBuilder.cs:** Permitted to reference `Chronicles.Documents.Internal` types (`IChangeFeedFactory`, `DocumentSubscription<T,P>`, `IDocumentSubscription`) for DI wiring of change-feed subscriptions. One-way, DI-only coupling.
2. **Chronicles.Tests:** Assembly has `InternalsVisibleTo` access to all internal types for test fakes and integration testing. Only permitted consumer of cross-layer internals.

**Files Changed:**
- `src/Chronicles/EventStore/IStateContext.cs` — added `Create()` factory
- `src/Chronicles/Cqrs/Internal/CommandProcessor.cs` — removed internal dependency
- `src/Chronicles/Cqrs/Internal/DocumentProjectionRebuilder.cs` — removed internal dependency
- `src/Chronicles/EventStore/DependencyInjection/EventStoreBuilder.cs` — documented exception
- `src/Chronicles/IsExternalInit.cs` — documented exception

### 3. Architecture Enforcement with NetArchTest — Duncan (2026-03-05)

**Status:** ✅ Implemented

**Decision:** Add architecture enforcement tests using **NetArchTest.Rules** (v1.3.2) to prevent layer-boundary violations.

**Implementation:**
- **Package:** NetArchTest.Rules v1.3.2 (added to `Directory.Packages.props`)
- **Test file:** `test/Chronicles.Tests/Architecture/LayerBoundaryTests.cs`
- **Test count:** 6 tests, all passing

**Tests enforce:**
- Documents cannot reference EventStore or Cqrs
- EventStore cannot reference Cqrs
- EventStore cannot reference Documents.Internal (except DI wiring)
- Cqrs cannot reference EventStore.Internal or Documents.Internal

**Explicit Exception:** `Chronicles.EventStore.DependencyInjection` namespace is permitted to reference `Chronicles.Documents.Internal` for DI wiring. Only permitted cross-internal-boundary reference.

**Benefits:**
- CI catches layering violations automatically
- Developers get fast feedback on architectural violations
- Explicit documentation of the one permitted exception (DI wiring)

---

## 4. CQRS Module Test Infrastructure — Chani (2026-03-04)

**Status:** ✅ Tests Created (Pending Execution & Bug Fixes)

**Mission:** Comprehensive test coverage for entire CQRS module.

**Deliverables:**
- `test/Chronicles.Tests/Cqrs/CommandContextExtensionsTests.cs` — 23 tests
- `test/Chronicles.Tests/Cqrs/StreamMetadataExtensionsTests.cs` — 11 tests
- Total: **34 tests** covering all extension methods and sentinel edge cases

**Critical Findings:**

#### Double-Emit Bug (VERIFIED)
**Location:** `CommandContextExtensions.cs` lines 78-79, 110-111
**Issue:** Event factory called twice in `respondWith` overloads
**Fix:** Reuse `evt` variable instead of calling `addEvent(context)` twice
**Test Evidence:** Factory call count = 2, but only 1 event written

#### Sentinel Bug (VERIFIED)
**Location:** `StreamMetadataExtensions.cs` line 34
**Issue:** Uses `!=` equality instead of `StreamVersion.IsValid()`
**Impact:** `StreamVersion.Any` rejected for non-empty streams; `RequireEmpty` and `RequireNotEmpty` sentinels fail validation
**Fix:** Replace `metadata.Version != requiredVersion` with `!metadata.Version.IsValid(requiredVersion)`

**Execution Blocker:** .NET SDK 10.0.200-preview NuGet bug ("Ambiguous project name 'Chronicles'"). Tests will execute on stable SDK.

**Assigned Tasks:**
- Gurney: Implement both bug fixes
- Thufir: Investigate SDK issue; verify tests on stable SDK
- Chani: Run tests after fixes to confirm sentinel behavior

---

## 5. Public API Surface Audit — Gurney (2026-03-04)

**Status:** ✅ Complete

**Audit Scope:** All public API types in `src/Chronicles/`

**Key Finding:**
- **ONE issue identified:** `EventDocumentBase` should be `internal` (currently `public`)
- All other public APIs are correctly designed and segregated
- `InternalsVisibleTo` properly configured for test access

**Public API Surface Summary:**
- ✅ 8 core EventStore interfaces
- ✅ 13 core CQRS interfaces
- ✅ 8 core Documents interfaces
- ✅ 17 value types & records
- ✅ 15 configuration classes
- ✅ 8 result types & delegates
- ✅ 6 enumerations
- ✅ 8 extension methods
- ✅ 8 DI builders
- ✅ 18 testing helpers (intentionally public)
- ✅ 2 base classes

**Recommendation:** Change `EventDocumentBase` from `public abstract record` to `internal abstract record` before v1.0.0 release.

---

## 6. API Design Decisions for v1.0.0 — Thufir (2026-03-04)

**Status:** ✅ Reviewed & Decided

**Context:** Resolving 10 design concerns from Duncan's EventStore review.

### Accepted for v1.0.0 (4 items)

1. **EventId in EventMetadata** (additive)
   - Add `string? EventId = null` to `EventMetadata` record
   - Enables idempotency and deduplication for at-least-once delivery
   - Assigned to Gurney for implementation

2. **expectedVersion guard on DeleteStreamAsync** (additive)
   - Add `StreamVersion? expectedVersion = null` parameter
   - Validates stream version before deletion; throws `StreamConflictException` on mismatch
   - Provides concurrency safety for delete operations
   - Assigned to Gurney for implementation

3. **Implement CloseAsync** (behavior fix)
   - Complete stub implementation in `EventStreamWriter`
   - Update stream metadata to `StreamState.Closed` and persist
   - `Archived` state deferred to v1.x (remains commented out)
   - Assigned to Gurney for implementation

4. **Extend IEventSubscriptionExceptionHandler** (breaking, pre-release OK)
   - New signature: `HandleAsync(Exception exception, StreamEvent? evt, CancellationToken cancellationToken)`
   - Provides event context for dead-letter analysis and diagnostics
   - Existing implementations must update (acceptable for pre-release)
   - Assigned to Gurney for implementation

### Deferred to v1.x (4 items)

- **SchemaVersion in EventMetadata** — Additive, lower priority than EventId
- **CreatedAt in StreamMetadata** — Nice-to-have, not essential for core event sourcing
- **Rename ICommandHandler<TCommand> → IStreamCommandHandler<TCommand>** — Breaking change; defer to v2
- **IQueryHandler<TQuery, TResult> abstraction** — Additive; let users choose read-side patterns

### Doc-only (2 items)

- **TCommand parameter in ConsumeEvent** — Document design rationale: enables command-scoped projections for performance optimization
- **Read-side architecture** — Document that Chronicles uses direct Cosmos access via IDocumentProjection; no prescribed query pattern

---

## 7. Public Release Infrastructure — Stilgar (2026-03-04)

**Status:** ✅ Complete

**Changes Implemented:**

1. **NuGet Package Metadata** (Directory.Build.props + Chronicles.csproj)
   - Complete package metadata block with description, license (MIT), repository links
   - PackageIcon: icon.png verified in docs/images/
   - PackageReadmeFile: readme.md from repo root
   - Release config: GenerateDocumentation, GeneratePackageOnBuild, symbol packages (.snupkg)

2. **Fixed Duplicate Test Report Step** (release.yml)
   - Removed duplicate "📋 Test Report" step

3. **Added Dependabot Configuration** (.github/dependabot.yml)
   - NuGet updates: weekly, limit 5 PRs, "dependencies" label
   - GitHub Actions updates: weekly, limit 5 PRs, "dependencies" label

**Verification:**
- ✅ All XML files validate
- ✅ YAML structure correct
- ✅ icon.png exists
- ✅ readme.md at repo root

---

## 8. Squad Directory Git Tracking Policy — Stilgar (2026-03-06)

**Status:** ✅ Implemented (PR #18)

**Issue:** `.squad/` directory was tracked in git across branches, violating branch protection rules.

**Solution:**
```bash
git rm --cached -r .squad/
git commit -m "chore: remove .squad/ from git tracking (internal team state)"
```

**Permanent Policy:**
- Development branches: Squad may commit `.squad/` artifacts
- Protected branches (main, preview, insider): `.squad/` must not reach these branches
- Guard workflow (`squad-main-guard.yml`) enforces path blocking
- Local environment: `.squad/` lives in working tree, protected by `.gitignore`

**Verification:**
- ✅ `.squad/` no longer in `git ls-files`
- ✅ Directory still exists on disk (Squad operations unaffected)
- ✅ PR #18 clean (no team artifacts)

---

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
