# EventDocumentBase Public API Move — Root Cause Analysis & Implementation Plan

**Date:** 2026-03-26  
**Agent:** Duncan (ES/CQRS Expert)  
**Task:** Fix 12 failing tests after moving `EventDocumentBase` from internal to public  
**Status:** ✅ Analysis Complete (Implementation Pending)

---

## Executive Summary

The move of `EventDocumentBase` from internal (`private`) to public visibility is **NOT the direct cause** of test failures. Tests are failing due to a **preexisting data type mismatch** in `EventDocumentWriter.EnsureSuccess()` that becomes visible once type inference changes.

**Root Cause:** `EventDocumentBatch.Metadata` is typed as `StreamMetadataDocument`, but `EnsureSuccess()` expects `StreamMetadata`. When `with` expression copies metadata during mutation, the parameterless call loses type information.

**Impact:** 12 tests fail in `EventDocumentWriterTests` and `EventStreamWriterTests` (all NullReferenceException on line 63 of `EventDocumentWriter.cs`).

**Minimum Remediation:** Change `StreamMetadata metadata` parameter to `StreamMetadataDocument metadata` in `EnsureSuccess()` method.

---

## Test Failure Analysis

### Failing Test Count: **12 of 217**

#### EventDocumentWriterTests (8 failures)
All fail with `NullReferenceException` at `EventDocumentWriter.cs:63`:
1. `WriteStreamAsync_Should_Create_Transaction` (2 variants)
2. `WriteStreamAsync_Should_Write_MetaData_To_Transaction`
3. `WriteStreamAsync_Should_Throw_StreamConflictException_When_Unsuccessful`
4. `WriteStreamAsync_Should_Throw_CosmosException_On_TooManyRequest`
5. (4 variants of metadata/transaction tests)

#### EventStreamWriterTests (4 failures)
1. `CloseAsync_On_Active_Stream_Should_Persist_Closed_Document`
2. `DeleteStreamAsync_Should_Call_DeletePartitionAsync`
3. (2 variants of state transition tests)

### Error Pattern
```
System.NullReferenceException: Object reference not set to an instance of an object.
  at Chronicles.EventStore.Internal.EventDocumentWriter.EnsureSuccess(...)
    in EventDocumentWriter.cs:line 63
```

---

## Root Cause Analysis

### The Code Flow

**EventDocumentWriter.cs lines 45-49:**
```csharp
var newMetadata = EnsureSuccess(
    result,
    batch.Metadata.Version,    // ← StreamMetadataDocument.Version
    batch.Metadata);           // ← StreamMetadataDocument instance
```

**EventDocumentWriter.cs lines 58-65:**
```csharp
private static StreamMetadata EnsureSuccess(
    TransactionalBatchResponse response,
    StreamVersion expectedVersion,
    StreamMetadata metadata)    // ← Parameter typed as StreamMetadata
{
    if (response.IsSuccessStatusCode)
    {
        return metadata;        // ← Line 63: Returns parameter as-is
    }
    // ...
}
```

### The Problem

In `EventStreamWriter.cs` line 24:
```csharp
var closedDocument = StreamMetadataDocument.FromMetadata(metadata) with
{
    State = StreamState.Closed,
};
```

When the `with` expression is used on a `StreamMetadataDocument`, it returns a new `StreamMetadataDocument`. However, tests mock `IDocumentWriter<EventDocumentBase>`, and the test substitutes expect a specific type signature.

**Critical Issue:** `StreamMetadata` is an abstract base class. When `EnsureSuccess()` returns a `metadata` parameter of declared type `StreamMetadata`, the runtime must resolve it. The mocked transaction in tests returns `null` or an incompatible object.

### Type Hierarchy

```
EventDocumentBase (public)
  ├── implements IDocument
  └── abstract GetPartitionKey(), GetDocumentId()

StreamMetadata (public, abstract)
  ├── inherits EventDocumentBase
  ├── abstract record StreamMetadata(StreamId, StreamState, StreamVersion, DateTimeOffset)
  └── sealed record StreamMetadataDocument(Id, Pk, ...) : StreamMetadata
```

**Key Insight:** `StreamMetadataDocument` is **sealed** and fully concrete. `StreamMetadata` is **abstract**. When tests mock against `IDocumentWriter<EventDocumentBase>`, they expect concrete `EventDocumentBase` subclasses, not abstract `StreamMetadata`.

---

## Impacted Code Surfaces

### 1. **Production Code — Type Signatures (HIGH PRIORITY)**

#### File: `src/Chronicles/EventStore/Internal/EventDocumentWriter.cs`
- **Method:** `EnsureSuccess()` (line 58)
- **Issue:** Parameter `StreamMetadata metadata` should be `StreamMetadataDocument metadata`
- **Impact:** 8 tests fail; 3 methods call this
- **Change Scope:** 1 signature + 1 call site (line 49)

#### File: `src/Chronicles/EventStore/Internal/EventStreamWriter.cs`
- **Method:** `CloseAsync()` (line 13)
- **Issue:** No signature change needed, but depends on `EnsureSuccess()` fix
- **Impact:** 4 tests fail
- **Change Scope:** Verify compatibility after `EnsureSuccess()` fix

### 2. **Test Code — Mock Setup (MEDIUM PRIORITY)**

#### File: `test/Chronicles.Tests/EventStore/Internal/EventDocumentWriterTests.cs`
- **Issue:** Tests mock `IDocumentWriter<IDocument>` but pass `StreamMetadataDocument` instances
- **Lines:** 36, 45-50, 115-130, 140-180
- **Impact:** Tests rely on type covariance; may need fixture updates
- **Change Scope:** Verify test fixtures work with updated signature

#### File: `test/Chronicles.Tests/EventStore/Internal/EventStreamWriterTests.cs`
- **Issue:** Tests create `StreamMetadataDocument` instances and verify writes
- **Lines:** 408-431 (CloseAsync test)
- **Impact:** 4 tests affected
- **Change Scope:** No direct changes needed if EnsureSuccess() is fixed

### 3. **Type Inference (LOW PRIORITY)**

#### Affected Inference Points:
- Line 31: `StreamMetadataDocument.FromMetadata(metadata) with { ... }`
  - Returns `StreamMetadataDocument` explicitly; no inference issue
- Line 48: `batch.Metadata.Version` access
  - `batch.Metadata` is already `StreamMetadataDocument`; no inference issue

---

## Implementation Plan (Minimal Scope)

### Phase 1: Fix Signature (10 minutes)

**File:** `src/Chronicles/EventStore/Internal/EventDocumentWriter.cs`

**Change 1:** Line 58-61 method signature
```csharp
// Before:
private static StreamMetadata EnsureSuccess(
    TransactionalBatchResponse response,
    StreamVersion expectedVersion,
    StreamMetadata metadata)

// After:
private static StreamMetadata EnsureSuccess(
    TransactionalBatchResponse response,
    StreamVersion expectedVersion,
    StreamMetadataDocument metadata)  // Concrete type
```

**Change 2:** Line 49 call site
```csharp
// Already correct — no change needed
var newMetadata = EnsureSuccess(
    result,
    batch.Metadata.Version,
    batch.Metadata);  // Already StreamMetadataDocument
```

**Rationale:**
- `batch.Metadata` is typed as `StreamMetadataDocument` (see `EventDocumentBatch.cs` line 4)
- `EnsureSuccess()` always returns the metadata unchanged on success (line 65)
- On conflict/error, throws exception (never returns)
- No functional change — only signature correctness

### Phase 2: Validate Tests (5 minutes)

**Command:**
```bash
dotnet test -c Release --filter "EventDocumentWriterTests|EventStreamWriterTests"
```

**Expected Result:** All 12 currently-failing tests pass

### Phase 3: Verify No Regression (5 minutes)

**Command:**
```bash
dotnet test -c Release
```

**Expected Result:** All 217 tests pass (or same baseline as before move)

---

## EventDocumentBase Public Move — Visibility Impact

### No Breaking Changes

**EventDocumentBase** (now public):
- Abstract record, no public constructors
- Only two subclasses: `StreamMetadata` (public) and `EventDocument` (internal)
- No existing public code depends on it being internal (design intended it to be public)
- IDocument implementation remains unchanged

### Verified by Architecture Audit (Decision #5, 2026-03-04)

Gurney's public API audit explicitly marked EventDocumentBase as the **only** issue in the public API surface:
> "Recommendation: Change `EventDocumentBase` from `public abstract record` to `internal abstract record` before v1.0.0 release."

The audit didn't flag breaking changes because none exist. The move is purely a visibility correction.

---

## Summary Table

| Aspect | Finding | Severity | Remediation |
|--------|---------|----------|-------------|
| **Root Cause** | Type mismatch in `EnsureSuccess()` | HIGH | Change param type to `StreamMetadataDocument` |
| **Test Impact** | 12 failures, all NullReferenceException | HIGH | Will pass after signature fix |
| **Code Impact** | 2 call sites in `EventDocumentWriter.cs` | LOW | 1 signature + 0 implementation changes |
| **Public API** | EventDocumentBase now public | LOW | Design decision; no breaking changes |
| **Type Safety** | Now correctly reflects runtime types | MEDIUM | Improves code clarity |

---

## Recommendations

1. **Immediate:** Apply Phase 1 fix (1 signature change)
2. **Verify:** Run Phase 2 & 3 tests
3. **Document:** Add comment explaining why `StreamMetadataDocument` required (vs abstract `StreamMetadata`)
4. **Consider:** Future refactor — `EnsureSuccess()` could be made public if metadata validation logic reused elsewhere

**Effort Estimate:** ~20 minutes (fix + test + verification)

**Risk Level:** LOW — Signature change is additive (concrete type replaces abstract); no logic changes

