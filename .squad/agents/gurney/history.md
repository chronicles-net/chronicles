# Gurney — History

## Learnings

### 2026-03-06: Multi-Name Event Registration API

**Context:** Implemented `AddEvent<TEvent>(string name, params string[] aliases)` overload per the Event Evolution PRD.

**Design Decisions:**
- Each alias gets its own `EventDataConverter(aliasName, typeof(TEvent))` — simplest approach, no new converter class needed. The default converter matches on `context.Metadata.Name == eventName`, so each alias converter independently matches its specific name.
- Alias registrations tracked as `List<(Type, string, IEventDataConverter)>` in `EventStoreBuilder` — supports re-registration cleanup via `RemoveAll` on same TEvent type.
- Conflict detection in `Build()` (not at registration time) — validates all primary names + aliases are unique across the entire catalog. Covers cross-type conflicts.
- `EventCatalog` takes optional `aliasMappings` constructor parameter (default null for backwards compat). Aliases added to `names` dict after primary names.
- `GetEventName(Type)` unchanged — returns primary name only. Aliases are read-only (deserialization only, never written).

**Files Changed:**
- `src/Chronicles/EventStore/DependencyInjection/EventStoreBuilder.cs` — new overload, alias tracking, validation
- `src/Chronicles/EventStore/Internal/EventCatalog.cs` — alias-aware constructor
- `src/Chronicles/EventStore/IEventDataConverter.cs` — clarified null-return XML docs

**Key Insight:** No `AliasedEventDataConverter` class needed. The existing `EventDataConverter` works perfectly — just instantiate one per name (primary or alias). The catalog's `names` dict makes them all reachable.

**Implementation Outcome (2026-03-06):**
- Feature shipped in PR #27 (feature/multi-name-event-registration)
- 7 new tests added (3 conflict detection + Chani's 4 alias tests)
- All 220 tests passing, 0 regressions
- Code review: APPROVED by Thufir
- Build: ✅ Green (Release configuration)
- Status: ✅ Ready for production merge
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

### 2026-03-05: StateContext Architectural Fix

**Context:** Fixed architectural violation where CQRS code (`CommandProcessor.cs`, `DocumentProjectionRebuilder.cs`) was referencing `Chronicles.EventStore.Internal.StateContext` — an internal type.

**Implementation:**
- Added `IStateContext.Create()` static factory method to provide public instantiation point
- Updated `CommandProcessor.cs`: removed `using Chronicles.EventStore.Internal;`, changed parameter type to `IStateContext`, replaced `new StateContext()` with `IStateContext.Create()`
- Updated `DocumentProjectionRebuilder.cs`: removed internal using, replaced instantiation with factory call
- Documented two explicit architectural exceptions:
  1. `EventStoreBuilder.cs` — permitted to reference `Documents.Internal` types for DI wiring of change-feed subscriptions
  2. `IsExternalInit.cs` — `Chronicles.Tests` granted access to all internal types for test fakes and integration testing

**Learnings:**
- `IStateContext.Create()` static factory added as the public instantiation point for IStateContext in EventStore
- StateContext violation fixed in CommandProcessor.cs and DocumentProjectionRebuilder.cs
- EventStoreBuilder.cs and IsExternalInit.cs have explicit exception comments

**Decision file:** `.squad/decisions/inbox/gurney-statecontext-fix.md`
