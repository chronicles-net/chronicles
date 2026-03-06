# Chani — History

## Learnings

### 2026-03-06: Event Evolution PRD — Test Coverage

**Work Completed:**
- Added 1 new test to `StreamEventConverterTests.cs`: verifies converter returning `null` produces `UnknownEvent` (critical gap identified in PRD section 4)
- Added 5 alias tests to `EventCatalogTests.cs` behind `#if ENABLE_ALIAS_TESTS` guard (pending Gurney's `EventCatalog` constructor change):
  - `GetConverter_Should_Return_Converter_For_Alias`
  - `GetConverter_Should_Return_Same_Converter_For_Primary_And_Alias`
  - `GetEventName_Should_Return_Primary_Name_Even_When_Aliases_Exist`
  - `Constructor_Should_Throw_On_Duplicate_Alias`
  - `Constructor_Should_Throw_On_Alias_Conflicting_With_Primary_Name`
- Confirmed `EventDataConverterTests.Convert_Should_Return_Null_When_EventName_Does_Not_Match` already exists

**Test Results:**
- Full suite: 214 tests pass, 0 failures, 0 warnings
- Alias tests compile-gated until Gurney adds second constructor parameter to EventCatalog

**Design Assumptions for Alias Tests:**
- EventCatalog will accept `IDictionary<string, IEventDataConverter>?` as second constructor parameter
- Aliases map to same converter as primary name
- `GetEventName(Type)` returns canonical name only
- Duplicate/conflicting aliases throw `InvalidOperationException`

**Verification Outcome (2026-03-06):**
- All design assumptions verified by Gurney's implementation
- Removed `#if ENABLE_ALIAS_TESTS` guards — all 5 alias tests now active
- Added EventStoreBuilderTests (3 conflict detection tests) by Gurney and Coordinator
- Total new tests: 6 (1 null-return + 5 alias scenarios)
- All 220 tests passing, 0 regressions
- Code review: APPROVED by Thufir
- Status: ✅ Test coverage complete, production-ready
