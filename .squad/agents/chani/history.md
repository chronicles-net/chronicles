# Chani — History

## Project Context
- **Project:** Chronicles — event sourcing + CQRS framework for .NET 10
- **Stack:** C#, .NET 10, Azure Cosmos DB (v3.55.0), Aspire, xunit.v3 (v3.2.0), Atc.Test (v2.0.16), coverlet
- **Repo:** chronicles-net/chronicles
- **User:** Lars Skovslund
- **Joined:** 2026-03-04

## Core Context
Test projects: `test/Chronicles.Tests/` (integration) and `test/Chronicles.Core.Tests/` (unit). Both target net10.0. CI uses `dotnet test --collect:"XPlat Code Coverage"`. Coverage badge auto-committed to `.github/coveragereport/`. TreatWarningsAsErrors in Release config.

## Learnings
<!-- Append entries here as you work -->
