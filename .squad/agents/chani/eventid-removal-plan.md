# EventId Removal from EventMetadata — Test & Documentation Impact Analysis

**Prepared by:** Chani (Tester)  
**Date:** 2026-03-25  
**Status:** Ready for Implementation  

## Executive Summary

EventId (nullable `string? EventId` property) was added to `EventMetadata` in v1.0.0 for idempotency and deduplication support. Removal requires:
- **Test deletion:** 3 tests in `EventMetadataTests.cs`
- **Documentation updates:** 2 documentation files + CHANGELOG
- **No regressions:** 9 test files use `EventMetadata.Empty` indirectly (no changes needed)
- **Coverage:** 100% safe removal — no product code or samples depend on EventId

---

## Part 1: Existing Tests to Remove

### File: `test/Chronicles.Tests/EventStore/EventMetadataTests.cs`

**Location:** Lines 8-26 (entire test class)

**Tests to delete (3 total):**

1. **`Empty_Has_Null_EventId`** (lines 8-9)
   - Asserts `EventMetadata.Empty.EventId` is null
   - **Action:** DELETE — EventId property will not exist

2. **`EventId_Can_Be_Set_Using_With_Syntax`** (lines 12-17)
   - Tests setting EventId via record with-syntax
   - **Action:** DELETE — EventId property will not exist

3. **`EventId_Is_Preserved_Through_Record_Copy`** (lines 20-26)
   - Tests EventId survives record copy
   - **Action:** DELETE — EventId property will not exist

**Outcome:** Delete entire file (contains only EventId tests)

---

## Part 2: Missing Regression Tests Worth Adding

### Regression Test 1: EventMetadata Record Without EventId

**File:** `test/Chronicles.Tests/EventStore/EventMetadataTests.cs` (if file retained for other metadata tests)

**Purpose:** Verify EventMetadata record can be constructed and serialized without EventId property

```csharp
[Fact]
public void EventMetadata_Constructs_Without_EventId()
{
    var metadata = new EventMetadata(
        Name: "OrderPlaced",
        CorrelationId: "corr-123",
        CausationId: "caus-456",
        StreamId: new StreamId("order", "ord-789"),
        Timestamp: DateTimeOffset.UtcNow,
        Version: new StreamVersion(1));
    
    metadata.Should().NotBeNull();
    metadata.Name.Should().Be("OrderPlaced");
}
```

**Rationale:** Ensure no breaking behavior when EventId is removed — EventMetadata must remain fully functional with only 6 core properties.

---

### Regression Test 2: EventMetadata.Empty Stability

**File:** Existing coverage in indirect tests (e.g., `StreamEventConverterTests.cs`)

**Purpose:** Verify `EventMetadata.Empty` continues to work as a default/zero-value construct

**Status:** ✅ Already covered by all 9 tests that use `EventMetadata.Empty`

---

## Part 3: Documentation & Guidance Becoming Stale

### Documentation File 1: `docs/testing.md`

**Section:** "API Changes in v1.0.0" → "EventId for Idempotency" (lines 538-552)

**Content to Remove:**
```markdown
### EventId for Idempotency

The `EventMetadata` record now includes an optional `EventId` property for 
deduplication and idempotency:

```csharp
// Write events with explicit IDs for deduplication
var eventId = Guid.NewGuid().ToString();
await writer.WriteAsync(streamId, new[]
{
    new OrderPlaced("ord-123", "cust-456", 99.99m, DateTimeOffset.UtcNow)
}, new StreamWriteOptions
{
    EventMetadata = new Dictionary<string, string> { ["EventId"] = eventId }
});
```
```

**Action:**
- DELETE the entire "EventId for Idempotency" subsection (lines 538-552)
- Update "General Best Practices" bullet (line 534): remove `- **Verify idempotency** when using \`EventId\` for deduplication scenarios`

**Rationale:** EventId no longer available; idempotency guidance becomes obsolete.

---

### Documentation File 2: `docs/event-store.md`

**Sections with EventId references (to be removed):**

1. **EventMetadata Properties** section (contains EventId description)
   - Remove: `- **EventId**: Optional — unique identifier for deduplication and idempotency in at-least-once delivery scenarios`
   - Remove: `Console.WriteLine($"EventId: {metadata.EventId}");  // For deduplication`

2. **Using EventId for Idempotency** subsection
   - Remove entire subsection with code example and explanation
   - Contains lines like: `{ EventId = eventId }  // Attach deduplication ID` and retry logic explanation

3. **Best Practices** section
   - Remove: `- **Use \`EventId\` for idempotent operations** — include a unique, stable ID when writing events to enable deduplication on retry`

**Action:**
- Locate and DELETE all EventId-related documentation blocks
- Keep all other event store documentation intact
- Update table of contents/headers if EventId subsection was major

**Rationale:** EventId is no longer a supported feature; documentation should not guide users to a non-existent API.

---

### Documentation File 3: `CHANGELOG.md`

**Section:** v1.0.0 release notes

**Line to remove:**
```markdown
- **Event IDs:** `EventId` field on `EventMetadata` for idempotency and deduplication support in at-least-once delivery scenarios
```

**Action:**
- Remove the EventId feature line from CHANGELOG
- Add entry in the removal section or under v1.0.1/later if documenting deprecation

**Rationale:** EventId is no longer in v1.0.0; changelog should reflect removal or never-shipped status.

---

## Part 4: Tests with No Changes Needed (EventMetadata.Empty Usage)

These 9 tests use `EventMetadata.Empty` as a convenient zero-value construct. They do **not** directly reference EventId and require **no changes**:

1. ✅ `test/Chronicles.Tests/Cqrs/CommandContextExtensionsTests.cs` — Uses `EventMetadata.Empty`
2. ✅ `test/Chronicles.Tests/Cqrs/CommandHandlerExecutorTests.cs` — Uses `EventMetadata.Empty`
3. ✅ `test/Chronicles.Tests/Cqrs/StatefulCommandExecutorTests.cs` — Uses `EventMetadata.Empty`
4. ✅ `test/Chronicles.Tests/Cqrs/StatelessCommandExecutorTests.cs` — Uses `EventMetadata.Empty`
5. ✅ `test/Chronicles.Tests/EventStore/Internal/DefaultEventSubscriptionExceptionHandlerTests.cs` — Uses `EventMetadata.Empty`
6. ✅ `test/Chronicles.Tests/EventStore/Internal/EventDataConverterTests.cs` — Uses `EventMetadata.Empty` (via AutoFixture)
7. ✅ `test/Chronicles.Tests/EventStore/Internal/EventDocumentWriterTests.cs` — Uses `EventMetadata` (via AutoFixture)
8. ✅ `test/Chronicles.Tests/EventStore/Internal/EventStreamProcessorTests.cs` — Uses `EventMetadata` (via AutoFixture)
9. ✅ `test/Chronicles.Tests/EventStore/Internal/StreamEventConverterTests.cs` — Uses `EventMetadata` (via AutoFixture)

---

## Part 5: Validation Commands to Prove Safety

### Command 1: Pre-Removal Baseline

```bash
# Run all tests before removing EventId — establish baseline
dotnet test -c Release --collect:"XPlat Code Coverage" --logger:"trx;LogFileName=baseline.trx"
```

**Expected outcome:**
- All 220+ tests pass
- EventMetadataTests.cs: 3 passing tests (will be removed)
- Code coverage baseline documented

---

### Command 2: Post-Removal Verification

```bash
# After removing EventId and updating documentation:
dotnet test -c Release --collect:"XPlat Code Coverage" --logger:"trx;LogFileName=after-removal.trx"
```

**Expected outcome:**
- 217+ tests pass (220 - 3 deleted tests)
- No new failures in any other test file
- Code coverage should remain equivalent or improve (fewer false positives in EventMetadata)

---

### Command 3: Build Validation (Compile-Time Safety)

```bash
# Verify no dead references to EventId remain
dotnet build -c Release -Werror
```

**Expected outcome:**
- 0 errors
- 0 warnings
- All projects compile cleanly

---

### Command 4: Code Search Verification

```powershell
# Confirm no remaining EventId references in test code or docs
rg "EventId" --glob="*.cs" --glob="*.md" -- E:\source\private\chronicles-net\chronicles\test E:\source\private\chronicles-net\chronicles\docs
```

**Expected outcome:**
- 0 matches (after removal)
- Proves clean removal, no orphaned references

---

## Part 6: Test Coverage Impact Assessment

### Coverage Regression Risk: **LOW**

**Removed tests (3 total):**
- `Empty_Has_Null_EventId` — Tests null state of removed property
- `EventId_Can_Be_Set_Using_With_Syntax` — Tests removed feature
- `EventId_Is_Preserved_Through_Record_Copy` — Tests removed feature

**Impact:** ✅ No coverage loss — these tests are 100% tied to EventId functionality. Removing them removes the feature they test.

### Indirect Test Stability: **HIGH**

**Why 9 other tests unaffected:**
- All use `EventMetadata.Empty` or AutoFixture-generated instances
- EventId removal does not change constructor signature or default behavior
- EventMetadata.Empty now simply has 6 properties instead of 7 (no breaking change)

---

## Part 7: Architecture & Documentation Review Checklist

- [ ] Confirm EventId removed from product code (`src/Chronicles/EventStore/EventMetadata.cs`)
- [ ] Delete `test/Chronicles.Tests/EventStore/EventMetadataTests.cs`
- [ ] Update `docs/testing.md` — remove EventId subsection & best practice
- [ ] Update `docs/event-store.md` — remove all EventId references
- [ ] Update `CHANGELOG.md` — remove EventId feature announcement
- [ ] Run `dotnet build -c Release` — 0 errors, 0 warnings
- [ ] Run `dotnet test -c Release` — verify pass count (220 → 217)
- [ ] Run code coverage — confirm no regression
- [ ] Search codebase for orphaned "EventId" references — expect 0 matches

---

## Conclusion

**Removal Safety: ✅ HIGHLY SAFE**

EventId removal is straightforward because:
1. **Isolated:** Only 3 direct tests, 0 product code usage
2. **Non-breaking:** Removal is additive-only (property removed, no signature changes)
3. **Well-documented:** Clear list of files to modify
4. **Regression-proof:** 9 indirect tests verify core EventMetadata behavior unchanged

**Estimated implementation time:** 30 minutes (delete tests, update docs, verify)

