# Duncan — History

## Project Context
- **Project:** Chronicles — event sourcing + CQRS framework for .NET 10
- **Stack:** C#, .NET 10, Azure Cosmos DB (v3.55.0), Aspire, xunit.v3, OpenTelemetry
- **Repo:** chronicles-net/chronicles
- **User:** Lars Skovslund
- **Joined:** 2026-03-04

## Core Context
Event flow in Chronicles: `IEventStreamWriter` appends `StreamEvent` to Cosmos DB. `IEventProcessor` reads the stream and applies events to state via `IStateProjection<TState>`. Document projections (`IDocumentProjection<TDocument>`) persist read models. CQRS: `ICommandHandler<TCommand>` is the write-side; projections are the read-side. `StreamMetadata` carries stream-level context.

## Learnings
<!-- Append entries here as you work -->
