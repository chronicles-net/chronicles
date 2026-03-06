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
