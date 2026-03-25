## EventDocumentBase test alignment

**Date:** 2026-03-25  
**Owner:** Gurney

### Context

`EventDocumentWriter` and `EventStreamWriter` now depend on `IDocumentWriter<EventDocumentBase>`, but the corresponding EventStore tests were still freezing `IDocumentWriter<IDocument>` and `IDocumentTransaction<IDocument>`.

That stale generic mismatch meant AutoFixture/NSubstitute supplied different substitutes than the SUT actually consumed, which explained the failing delete/close assertions and transaction-path failures after the `EventDocumentBase` move.

### Decision

Treat this as a **test wiring correction**, not a production EventStore change:

1. Update writer tests to freeze the exact closed generic used by production:
   - `IDocumentWriter<EventDocumentBase>`
   - `IDocumentTransaction<EventDocumentBase>`
2. Scan adjacent EventStore tests/helpers for the same stale assumption and stop once no additional `IDocumentWriter<IDocument>`/`IDocumentTransaction<IDocument>` usages remain in EventStore test paths.
3. Do **not** change `EventDocumentWriter.EnsureSuccess(...)` unless the corrected tests still expose a production fault.

### Verification

- `dotnet build .\chronicles.sln -c Release` ✅
- `dotnet test .\test\Chronicles.Tests\Chronicles.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~EventDocumentWriterTests|FullyQualifiedName~EventStreamWriterTests"` ✅ (23/23 passing)

### Result

The fix stayed entirely in test code. Once the test doubles matched the real `EventDocumentBase` contract, the targeted writer tests passed without any `EnsureSuccess(...)` production cleanup.
