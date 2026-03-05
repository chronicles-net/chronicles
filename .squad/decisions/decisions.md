# Decisions Log

## 2026-03-05: Architectural Layer Enforcement Directive

**Date:** 2026-03-05T06:46:09Z  
**By:** Lars Skovslund (via Copilot)  
**Status:** Adopted

### Decision

The architectural layering must always be enforced:

1. **Documents** — lowest level, the foundation
2. **EventStore** — built on top of Documents; may only use public interfaces and classes from Documents
3. **CQRS** — built on top of EventStore; may only use public interfaces and types from EventStore and Documents

Any violation (a higher layer referencing internal/non-public types from a lower layer, or a layer skipping a layer) is forbidden unless explicitly documented as an exception.

### Rationale

User request — captured for team memory and enforcement in all code review and development decisions.
