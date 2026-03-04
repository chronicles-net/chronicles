# Changelog

All notable changes to **Chronicles** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2026-03-04

### Added

- Event sourcing: append-only event streams with optimistic concurrency (`StreamId`, `IEventStreamWriter`, `IEventStreamReader`)
- CQRS: command handlers with state projections (`ICommandHandler<TCommand>`, `IStatelessCommandHandler<TCommand>`, `IStateProjection<TState>`)
- Document Store: read-model projections via Cosmos DB change feed (`IDocumentProjection<TDocument>`)
- `EventId` field on `EventMetadata` for idempotency and deduplication support
- `expectedVersion` parameter on `DeleteStreamAsync` for safe concurrent deletion
- `CloseAsync` on `IEventStreamWriter` to mark streams as permanently closed
- Cosmos DB change-feed subscriptions for real-time event processing
- Built-in Aspire integration for local development and testing
- Testing helpers: `FakeContainer`, `FakeDocumentStore`, `FakeEventStreamWriter` for unit testing without Cosmos

### Fixed

- `StreamVersion.IsValid()` now correctly handles sentinel values (`Any`, `RequireEmpty`, `RequireNotEmpty`)
- `StreamMetadataExtensions.EnsureSuccess()` uses `IsValid()` instead of raw equality comparison
- `IEventSubscriptionExceptionHandler.HandleAsync` extended with `StreamEvent?` and `CancellationToken` for richer diagnostics

[1.0.0]: https://github.com/chronicles-net/chronicles/releases/tag/v1.0.0
