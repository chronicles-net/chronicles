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
- **NuGet Metadata Structure**: Package metadata should be centralized in `Directory.Build.props` under a conditional PropertyGroup (`Condition="'$(IsPackable)' != 'false'"`) to apply only to packable projects. Test projects have `<IsPackable>false</IsPackable>` to exclude them from packaging.
- **Package Icon & Readme in Release**: When configuring NuGet packaging for public release, include `PackageIcon` and `PackageReadmeFile` properties, and add corresponding `<None>` ItemGroup entries in the `.csproj` with `Pack="true"` and `PackagePath="\"` to embed them in the `.nupkg`.
- **Symbol Package Generation**: For NuGet.org submission, use `<IncludeSymbols>true</IncludeSymbols>` and `<SymbolPackageFormat>snupkg</SymbolPackageFormat>` to generate separate `.snupkg` files for debugging symbols.
- **Documentation File Generation**: Add `<GenerateDocumentationFile>true</GenerateDocumentationFile>` in Release PropertyGroup to create XML documentation for API intellisense in downstream projects.
