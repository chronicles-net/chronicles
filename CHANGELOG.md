# Changelog

All notable changes to **Chronicles** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Removed

- **Event metadata:** Removed `EventMetadata.EventId` from the public API. This is a breaking change for consumers that accessed this property directly.

## [1.0.0] - 2026-03-06

### Added

- **Event sourcing:** Append-only event streams with optimistic concurrency (`StreamId`, `IEventStreamWriter`, `IEventStreamReader`)
- **CQRS:** Command handlers with state projections (`ICommandHandler<TCommand, TState>`, `IStatelessCommandHandler<TCommand>`, `IStateProjection<TState>`)
- **Document Store:** Read-model projections via Cosmos DB change feed (`IDocumentProjection<TDocument>`, `IDocumentReader<T>`, `IDocumentWriter<T>`)
- **Event Evolution:** Backward-compatible event schema evolution with aliases, upcasting, and fault-tolerant deserialization
- **Stream deletion safety:** `expectedVersion` parameter on `DeleteStreamAsync` for safe concurrent deletion with optimistic concurrency
- **Stream close:** `CloseAsync` on `IEventStreamWriter` to mark streams as permanently closed without deleting events
- **Change-feed subscriptions:** Cosmos DB change-feed integration for real-time event processing via `IEventProcessor`
- **Aspire integration:** Built-in support for .NET Aspire for local development and end-to-end testing
- **Testing infrastructure:** `AddFakeChronicles`, `FakeContainer`, `FakeDocumentStore`, `FakeEventStreamWriter` for fast in-memory unit testing without Cosmos DB
- **Architecture enforcement:** NetArchTest integration to prevent layer-boundary violations between Documents → EventStore → CQRS
- **Comprehensive documentation:** 9 guides covering getting started, event store patterns, CQRS handlers, projections, document store, event subscriptions, event evolution, dependency injection, and testing

### Fixed

- `StreamVersion.IsValid()` now correctly handles sentinel values (`Any`, `RequireEmpty`, `RequireNotEmpty`)
- `StreamMetadataExtensions.EnsureSuccess()` uses `IsValid()` instead of raw equality comparison to prevent false rejections
- `IEventSubscriptionExceptionHandler.HandleAsync` extended with `StreamEvent?` and `CancellationToken` for richer dead-letter diagnostics

### Changed

- **IEventSubscriptionExceptionHandler signature:** Now accepts `(Exception exception, StreamEvent? evt, CancellationToken cancellationToken)` for better event context during error handling

[1.0.0]: https://github.com/chronicles-net/chronicles/releases/tag/v1.0.0
