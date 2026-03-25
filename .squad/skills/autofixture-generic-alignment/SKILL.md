---
name: "autofixture-generic-alignment"
description: "Align AutoFixture/NSubstitute frozen dependencies with exact closed generic contracts"
domain: "testing"
confidence: "high"
source: "gurney eventdocumentbase fix 2026-03-25"
---

## Context

Chronicles tests use AutoFixture with NSubstitute. When a system under test depends on a closed generic service such as `IDocumentWriter<EventDocumentBase>`, the frozen substitute in the test must use that exact closed generic type or AutoFixture will inject a different substitute into the constructor.

---

## Patterns

### Freeze the exact closed generic

If production depends on:

```csharp
IDocumentWriter<EventDocumentBase>
```

then the test must freeze:

```csharp
[Frozen] IDocumentWriter<EventDocumentBase> writer
```

not:

```csharp
[Frozen] IDocumentWriter<IDocument> writer
```

Even though `EventDocumentBase` implements `IDocument`, those are different closed generic services and AutoFixture will not treat them as interchangeable.

### Mirror nested generic collaborators too

When the root dependency creates another generic collaborator, freeze or request the matching nested type as well:

```csharp
IDocumentTransaction<EventDocumentBase> transaction
```

Using `IDocumentTransaction<IDocument>` creates the same mismatch and breaks `Received(...)` assertions on transaction operations.

### Prefer targeted scans after contract moves

After changing a production generic contract, scan the adjacent tests for stale assumptions:

```text
IDocumentWriter<IDocument>
IDocumentTransaction<IDocument>
```

This catches wiring-only failures quickly before considering production fixes.

---

## Examples

### EventStore writer tests

Files:
- `test/Chronicles.Tests/EventStore/Internal/EventDocumentWriterTests.cs`
- `test/Chronicles.Tests/EventStore/Internal/EventStreamWriterTests.cs`

Correct pattern:

```csharp
[Frozen] IDocumentWriter<EventDocumentBase> writer,
IDocumentTransaction<EventDocumentBase> transaction,
```

---

## Anti-Patterns

- **Freezing a base-interface closed generic** — `IDocumentWriter<IDocument>` will not be injected where the SUT requests `IDocumentWriter<EventDocumentBase>`.
- **Changing production error handling before fixing test wiring** — verify the substitute graph first; stale generic freezes can mimic production regressions.
