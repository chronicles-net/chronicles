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

### PR #18: v1.0.0 Release & Squad Setup (2026-03-06)

**Task:** Create PR from feature/add-squad → main

**Actions Taken:**
1. Reviewed git log: 13 commits covering event sourcing, CQRS, architecture enforcement, packaging, and Squad initialization
2. Audited .squad/ directory: Found 30+ internal team state files tracked in git (violations of branch protection rules)
3. **Cleaned up .squad/** via `git rm --cached -r .squad/` with explanatory commit "chore: remove .squad/ from git tracking (internal team state)"
4. Pushed feature/add-squad to remote
5. Created PR #18 with comprehensive body covering:
   - Event sourcing & CQRS feature summary
   - Architecture enforcement (NetArchTest, layer boundaries, StateContext public factory)
   - Bug fixes (StreamVersion sentinel, EnsureSuccess, exception handler)
   - NuGet packaging (icon, readme, symbols, doc XML)
   - Test coverage & documentation
   - Reviewer notes (no breaking changes, all tests pass, v1.0.0 first release)

**Outcome:** PR #18 successfully created and pushed to GitHub
- **URL**: https://github.com/chronicles-net/chronicles/pull/18
- **Status**: Ready for review
- **Security**: .squad/ files removed from PR; branch protection guard will not block merge

**Key Decision**: .squad/ directory must remain in local working tree (development context) but never tracked in git on production branches. The .gitignore already excludes .squad/orchestration-log/ and .squad/log/, but the root .squad/ directory was previously being tracked. This commit ensures future Squad workflows operate cleanly.
