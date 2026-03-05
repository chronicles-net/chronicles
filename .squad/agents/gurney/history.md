# Gurney — History

## Project Context
- **Project:** Chronicles — event sourcing + CQRS framework for .NET 10
- **Stack:** C#, .NET 10, Azure Cosmos DB (v3.55.0), Aspire, xunit.v3, OpenTelemetry
- **Repo:** chronicles-net/chronicles
- **User:** Lars Skovslund
- **Joined:** 2026-03-04

## Core Context
Key source files under `src/Chronicles/`: `EventStore/` (streaming), `Documents/` (Cosmos projections), `Cqrs/` (command handling). All use nullable reference types, file-scoped namespaces, and centralized DI extension methods. Build: `dotnet build`. Test: `dotnet test`.

## Learnings
<!-- Append entries here as you work -->

### 2026-03-04: Public API Surface Audit (v1.0.0 Pre-Release)

**Context:** Conducted comprehensive audit of `src/Chronicles/` public API surface before v1.0.0 release to NuGet.org.

**Key Findings:**
- **One issue identified:** `EventDocumentBase` in `src/Chronicles/EventStore/Internal/EventDocumentBase.cs` is marked `public` when it should be `internal`. It's an implementation detail (base class for `EventDocument` and `StreamMetadataDocument` Cosmos DB mappings).
- **InternalsVisibleTo:** Already configured for `Chronicles.Tests` and `DynamicProxyGenAssembly2` in `IsExternalInit.cs`.
- **Overall assessment:** Public API surface is clean and well-designed. All other implementation details are correctly marked `internal`.

**Public API Categories (confirmed correct):**
1. Core interfaces: `IEventStreamReader`, `IEventStreamWriter`, `ICommandHandler<T>`, `IStateProjection<T>`, `IDocumentProjection<T>`, etc.
2. Value types: `StreamId`, `StreamVersion`, `StreamEvent`, `StreamMetadata`, `EventMetadata`, `Checkpoint`, etc.
3. Configuration: `EventStoreOptions`, `CommandOptions`, `DocumentOptions`, `StreamReadOptions`, `StreamWriteOptions`, etc.
4. Extension methods: `CommandContextExtensions`, `StreamMetadataExtensions`, `DocumentReaderExtensions`, etc.
5. DI builders: `ChroniclesBuilder`, `EventStoreBuilder`, `CqrsBuilder`, `DocumentStoreBuilder`, etc.
6. Testing helpers: All `src/Chronicles/Testing/` types (intentionally public for consumer testing)

**Audit report:** `.squad/decisions/inbox/gurney-api-surface-audit.md`

**Next step:** Thufir to review, then implement fix for `EventDocumentBase` visibility.
