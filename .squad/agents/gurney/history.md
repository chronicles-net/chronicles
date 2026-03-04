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
