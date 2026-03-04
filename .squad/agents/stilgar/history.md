# Stilgar — History

## Project Context
- **Project:** Chronicles — event sourcing + CQRS framework for .NET 10
- **Stack:** C#, .NET 10, Azure Cosmos DB, Aspire.Hosting v13.0.0, GitHub Actions, coverlet, ReportGenerator
- **Repo:** chronicles-net/chronicles
- **User:** Lars Skovslund
- **Joined:** 2026-03-04

## Core Context
CI: `.github/workflows/ci.yml` on ubuntu-latest, 15min timeout. Steps: checkout → setup .NET 9.0.x → restore/build (Release) → test (XPlat coverage + TRX) → ReportGenerator badge. NuGet packaging active in Release. Aspire host: `sample/Chronicles.AppHost/`. Global SDK constraint: `global.json` (9.0.0+).

## Learnings
<!-- Append entries here as you work -->
