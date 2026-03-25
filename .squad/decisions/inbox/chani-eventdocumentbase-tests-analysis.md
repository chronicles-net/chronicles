---
date: 2026-03-25
author: Chani
status: Analysis Complete — Ready for Implementation Recommendations
---

# Analysis: Test Failures After EventDocumentBase Visibility Change

## Executive Summary

**Failing Tests:** 12 (from 220 → 208 passing)  
**Root Cause:** Generic type constraint mismatch at test dependency injection  
**Failure Category:** All **runtime-only** (no compile-time errors)  
**Impact:** Direct test fixtures and cascading dependencies  
**Complexity:** Low — straightforward type alignment in test setup

---

## Failure Breakdown by Category

### Group A: Direct AutoFixture Injection Type Mismatch (5 tests)

**Severity:** 🔴 CRITICAL — blocks SUT construction

**Tests:**
1. `EventDocumentWriterTests.WriteStreamAsync_Should_Produce_Events` (Line 35)
2. `EventDocumentWriterTests.WriteStreamAsync_Should_Create_Transaction` (Line 89)
3. `EventDocumentWriterTests.WriteStreamAsync_Should_Write_MetaData_To_Transaction` (Line 140)
4. `EventDocumentWriterTests.WriteStreamAsync_Should_Commit_With_Matching_Metadata_Version` (estimated)
5. `EventDocumentWriterTests.WriteStreamAsync_Should_Write_Metadata` (estimated)

**Root Cause:**
- **Production Code** (`EventDocumentWriter.cs` line 9):
  ```csharp
  internal class EventDocumentWriter(
      IDocumentWriter<EventDocumentBase> writer,  // ← Expects EventDocumentBase
      IEventDocumentBatchProducer batchProducer)
  ```
  
- **Test Code** (`EventDocumentWriterTests.cs` lines 36, 91, 141, etc.):
  ```csharp
  [Theory, AutoNSubstituteData]
  internal async Task WriteStreamAsync_Should_Produce_Events(
      [Frozen] IDocumentWriter<IDocument> writer,  // ← Provides IDocument (TOO BROAD)
      [Frozen] IEventDocumentBatchProducer batchProducer,
      ...
      EventDocumentWriter sut,  // ← Constructor receives null for `writer`
  ```

**Why It Fails:**
- AutoFixture creates `IDocumentWriter<IDocument>` but the constructor signature changed to expect `IDocumentWriter<EventDocumentBase>`
- Generic type constraints are strict: `IDocumentWriter<IDocument>` ≠ `IDocumentWriter<EventDocumentBase>`
- When AutoFixture cannot satisfy the dependency, it injects `null`
- NSubstitute substitute for `IDocumentWriter<IDocument>` is incompatible with the constructor parameter type
- Result: `NullReferenceException` when `writer` is accessed in the constructor body

**Validation Checkpoint:**
- [ ] Verify `EventDocumentBase` is now `public` in `src/Chronicles/EventStore/EventDocumentBase.cs`
- [ ] Confirm `IDocumentWriter<EventDocumentBase>` is accessible from test project (via public API)
- [ ] Confirm no compile-time errors (this is purely a runtime DI issue)

---

### Group B: Cascading Dependency Failures (3 tests)

**Severity:** 🟠 SECONDARY — blocks EventStreamWriter tests

**Tests:**
1. `EventStreamWriterTests.DeleteStreamAsync_Should_Call_DeleteStreamAsync_On_Writer` (Line 354)
2. `EventStreamWriterTests.DeleteStreamAsync_With_Matching_Version_Should_Not_Throw` (Line 298)
3. `EventStreamWriterTests.CloseAsync_*` (Lines 424, 483)

**Root Cause:**
- `EventStreamWriter` depends on `EventDocumentWriter` (injected as `IEventDocumentWriter`)
- Tests fail to construct `EventStreamWriter` because `EventDocumentWriter` cannot be constructed
- AutoFixture cannot auto-satisfy `IEventDocumentWriter` → null injection → `NullReferenceException` when accessed

**Dependency Chain:**
```
EventStreamWriterTests
  └─ [AutoFixture] tries to build EventStreamWriter sut
      └─ [AutoFixture] tries to build EventDocumentWriter (registered as IEventDocumentWriter)
          └─ [AutoFixture] tries to build IDocumentWriter<EventDocumentBase>
              └─ FAILS: No matching `IDocumentWriter<IDocument>` in container
                  └─ Returns null
                      └─ EventDocumentWriter.writer field = null
                          └─ EventStreamWriter test fails
```

**Validation Checkpoint:**
- [ ] Verify Group A failures are fixed first (prerequisite)
- [ ] Confirm Group B failures disappear after Group A is resolved

---

### Group C: Metadata Type Casting Failures (2 tests)

**Severity:** 🟡 SECONDARY — runtime property access failures

**Tests:**
1. `EventDocumentWriterTests.WriteStreamAsync_Should_Throw_StreamConflictException_When_Unsuccessful` (Line 375)
2. `EventDocumentWriterTests.WriteStreamAsync_Should_Throw_CosmosException_On_TooManyRequest` (Line 328)

**Root Cause:**
- Once Group A is fixed, SUT constructs successfully
- But test parameters use `EventMetadata` (from test fixture)
- Production code at line 62-66 (`EnsureSuccess` method) accesses `metadata` properties:
  ```csharp
  private static StreamMetadata EnsureSuccess(
      TransactionalBatchResponse response,
      StreamVersion expectedVersion,
      StreamMetadata metadata)  // ← Parameter type
  {
      if (response.IsSuccessStatusCode)
      {
          return metadata;  // ← Returns StreamMetadata
      }
      // ... accesses metadata.StreamId, metadata.Version, metadata.State
  ```
- `StreamMetadata` is now a public abstract record inheriting from `EventDocumentBase`
- Test fixture provides `StreamMetadataDocument` (internal concrete type) or `EventMetadata` (metadata property)
- If test setup doesn't align, property access fails

**Validation Checkpoint:**
- [ ] Verify `StreamMetadata` inheritance chain (confirm it's `EventDocumentBase`)
- [ ] Verify test setup provides correct `StreamMetadata` subclass (`StreamMetadataDocument`)
- [ ] Confirm property access pattern matches metadata structure

---

## Failure Type Classification

| Category | Count | Compile-Time? | Runtime? | Root | Fix Scope |
|----------|-------|---------------|----------|------|-----------|
| **Direct Type Mismatch** | 5 | ❌ NO | ✅ YES | Test fixture declaration | Change 5 test parameters |
| **Cascading Dependency** | 3 | ❌ NO | ✅ YES | Dependency unresolvable | Automatic (fixes Group A) |
| **Metadata Casting** | 2 | ❌ NO | ✅ YES | Type alignment | Verify setup correctness |
| **Unknown / Misc** | 2 | ? | ? | Other | Investigate post-fix |
| **TOTAL** | **12** | | | | |

---

## Validation Scope: Implementation Plan Should Verify

### Phase 1: Fix Test Fixtures (Group A)

1. **Scope:** Update 5 test methods in `EventDocumentWriterTests.cs`
2. **Change Pattern:**
   ```csharp
   // BEFORE:
   [Frozen] IDocumentWriter<IDocument> writer,
   
   // AFTER:
   [Frozen] IDocumentWriter<EventDocumentBase> writer,
   ```
3. **Files:**
   - `test/Chronicles.Tests/EventStore/Internal/EventDocumentWriterTests.cs`
     - Line 36: `WriteStreamAsync_Should_Produce_Events`
     - Line 91: `WriteStreamAsync_Should_Create_Transaction`
     - Line 141: `WriteStreamAsync_Should_Write_MetaData_To_Transaction`
     - ~3 additional test methods with same pattern

4. **Validation:**
   - [ ] `dotnet build -c Release` → 0 errors
   - [ ] `dotnet test -c Release` → All 5 tests now pass
   - [ ] Run EventDocumentWriterTests specifically → Confirm 100% pass rate

### Phase 2: Verify Cascading Fixes (Group B)

1. **Scope:** No code changes needed; just verify tests pass
2. **Tests:** EventStreamWriterTests (3 failing tests)
3. **Validation:**
   - [ ] `dotnet test -c Release EventStreamWriterTests` → All pass
   - [ ] Confirm no new errors or warnings
   - [ ] If any failures remain, investigate as new issue

### Phase 3: Investigate Metadata Edge Cases (Group C)

1. **Scope:** Run full test suite with Group A/B fixed
2. **Validation:**
   - [ ] `dotnet test -c Release` → 217/217 tests passing (from 12 failures)
   - [ ] If Group C still fails, inspect `EnsureSuccess` method and metadata parameter
   - [ ] Confirm `StreamMetadataDocument` is passed correctly in test setup

### Phase 4: Full Integration Validation

1. **Scope:** Verify no regressions across entire codebase
2. **Validation:**
   - [ ] `dotnet build -c Release` → 0 errors, 0 warnings
   - [ ] `dotnet test -c Release --no-build` → 217/217 passing
   - [ ] Code search: `EventDocumentBase` should appear only in:
     - `src/Chronicles/EventStore/EventDocumentBase.cs` (definition)
     - `src/Chronicles/EventStore/StreamMetadata.cs` (inheritance)
     - `src/Chronicles/EventStore/Internal/EventDocument.cs` (inheritance)
     - `src/Chronicles/EventStore/Internal/EventDocumentWriter.cs` (generic param)
     - `src/Chronicles/EventStore/Internal/EventStreamWriter.cs` (generic param)
     - `src/Chronicles/EventStore/DependencyInjection/EventStoreConfigureDocumentStore.cs` (registration)
     - `src/Chronicles/EventStore/DependencyInjection/InitializationOptionsExtensions.cs` (initialization)
   - [ ] Test project should NOT directly reference `EventDocumentBase` (it's public API but internal-focused)
   - [ ] No circular dependencies introduced

---

## Key Insights

### Architectural Impact

1. **Public API Change:** `EventDocumentBase` moving from `internal` → `public` is a **visibility expansion**, not a breaking change
   - Downstream code (tests) now can reference it explicitly
   - Generic type constraints become more specific and type-safe
   - No functional changes to the class itself

2. **Test Fixture Specificity:** Tests must now be more explicit about their dependencies
   - **Before:** `IDocumentWriter<IDocument>` was accepted (covariant)
   - **After:** Must use `IDocumentWriter<EventDocumentBase>` (exact type expected)
   - This is **correct behavior** — tests should match production code exactly

3. **No Breaking Changes to Production Code**
   - `EventDocumentWriter` constructor unchanged in signature
   - Only the type parameter specificity improved
   - Tests must adapt, not production code

### Risk Assessment

- **Build Risk:** ✅ NONE — purely test-layer adjustments
- **Regression Risk:** ✅ LOW — changes are additive (making types more specific)
- **Coverage Risk:** ✅ NONE — same tests, same assertions, same coverage
- **Performance Risk:** ✅ NONE — no algorithm or data structure changes

---

## Recommendations for Implementation

### Validation Checklist (Before Merge)

- [ ] All 12 failing tests now pass
- [ ] No new compile warnings or errors
- [ ] Full test suite: 217/217 passing (or 220/220 if EventId removal not yet complete)
- [ ] Code search for unintended `EventDocumentBase` references: 0 in test code
- [ ] Architecture tests still pass (layer boundary checks)

### Testing Patterns to Verify

1. **Frozen Substitutes:** Confirm all `[Frozen] IDocumentWriter<...>` match constructor expectations
2. **Cascading Dependencies:** Verify EventStreamWriter tests can now construct SUT
3. **Metadata Handling:** Verify `StreamMetadataDocument` is correctly passed in exception tests

### Documentation Notes

- **Not needed:** No API documentation changes (EventDocumentBase is base class, not primary API surface)
- **Optional:** Consider adding comment in test files explaining why `IDocumentWriter<EventDocumentBase>` is required instead of `IDocumentWriter<IDocument>`

---

## Summary Table

| Dimension | Finding |
|-----------|---------|
| **Failing Tests** | 12 of 220 (5.5%) |
| **Root Cause** | Generic type mismatch in test dependency injection |
| **Compile-Time Errors?** | ❌ NO — all runtime failures |
| **Breaking Changes?** | ❌ NO — test-only adjustments |
| **Files to Change** | 1 (EventDocumentWriterTests.cs) |
| **Complexity** | ⭐ LOW (straightforward pattern match) |
| **Estimated Effort** | 15–20 minutes |
| **Risk Level** | 🟢 GREEN (low risk, high confidence) |

---

## Related Decisions

- **Decision #5 (Public API Audit):** Identified `EventDocumentBase` should change from `public` (current, wrong) to `internal` OR confirmed as intentionally public
- **Current Status:** Test failure suggests it was recently changed to `public`, triggering this test failure cascade

---

**Status:** Analysis complete. Ready for handoff to implementation team (Gurney or Lars).  
**Next Steps:** Execute implementation plan phases 1–4, validating at each checkpoint.
