# Squad Decisions

## Active Decisions

### 1. EventStore Design Review — Duncan (2026-03-04)

**Context:** Comprehensive review of `src/Chronicles/EventStore/` and `src/Chronicles/Cqrs/` identified 2 critical bugs and 10 design concerns requiring team decision.

**Critical Bugs:**
- **AddEventWhen double-emit** (`CommandContextExtensions.cs`): Event factory called twice in `respondWith` overloads, writing duplicate events per command. **Assigned:** Gurney (fix), Chani (regression test).
- **EnsureSuccess version check** (`StreamMetadataExtensions.cs`): Raw equality check conflicts with `StreamVersion.Any` sentinel, incorrectly rejecting non-empty streams. **Action:** Gurney to evaluate sentinel redesign (Options A/B/C).

**Design Concerns Requiring Decision:**
1. Add `string? EventId` to `EventMetadata` for idempotency and deduplication in at-least-once delivery scenarios
2. Add `int? SchemaVersion` to `EventMetadata` for explicit event upcasting support
3. Add `DateTimeOffset CreatedAt` to `StreamMetadata` for creation date queries
4. Add `StreamVersion? expectedVersion` or `StreamState? expectedState` guard to `DeleteStreamAsync`
5. Rename `ICommandHandler<TCommand>` to `IStreamCommandHandler<TCommand>` to clarify it is stateful (not stateless)
6. Remove `TCommand` parameter from `ConsumeEvent` signature or justify its presence
7. Decide: Implement `CloseAsync` and `Archived` state or remove from public API as incomplete stubs
8. Add `IQueryHandler<TQuery, TResult>` abstraction for read-side queries or document direct document store access as intended pattern
9. Extend `IEventSubscriptionExceptionHandler.HandleAsync(Exception)` to include `StreamEvent?` and `CancellationToken` for dead-letter analysis
10. Document implicit read-side architecture: direct Cosmos document access vs explicit query modeling

**Source:** `.squad/decisions/inbox/duncan-eventstore-review.md` (merged 2026-03-04T15:41)

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
