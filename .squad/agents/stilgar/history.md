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

### PR #35: EventId Removal from EventMetadata (2026-03-25)

**Task:** Create feature branch, commit EventId removal changes (product code + docs + tests), push branch, create PR, and request Copilot review on GitHub.

**Actions Taken:**
1. Created feature branch `feature/remove-eventid` from `main`
2. Staged ONLY product changes (preserved .squad/ modifications in working tree):
   - `src/Chronicles/EventStore/EventMetadata.cs` — removed EventId property + XML docs
   - `test/Chronicles.Tests/EventStore/EventMetadataTests.cs` — deleted file
   - `docs/event-store.md` — removed EventId references
   - `docs/testing.md` — removed EventId references
   - `CHANGELOG.md` — moved EventId from v1.0.0 Added to Unreleased Removed
3. Created conventional commit with message body explaining rationale and required Copilot trailer:
   ```
   Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
   ```
4. Pushed feature branch to origin
5. Created PR #35 against main with comprehensive body linking Decision #9 (EventId removal approval)
6. **BLOCKED:** Attempted to add `copilot` user as reviewer — GitHub API confirms user does not exist
   - Verified via `gh api users/copilot` → 404 Not Found
   - Verified via organization membership list: actual members are `christianhelle`, `cortz`, `LarsSkovslund`, `rickykaare`, `stl93`
   - No GitHub user named "copilot" or "Copilot" exists on GitHub.com

**Outcome:** PR #35 created successfully; review assignment blocked by non-existent user
- **URL**: https://github.com/chronicles-net/chronicles/pull/35
- **Status**: Ready to merge (product changes complete); review request cannot be assigned to non-existent user
- **Blocker**: GitHub does not support assigning review to user `copilot`. Recommend assigning to one of: `christianhelle`, `cortz`, `LarsSkovslund`, `rickykaare`, `stl93`

### Retry Attempt: PR #35 Copilot Review (2026-03-25)

**Task:** Assign PR #35 to @copilot for review per user directive.

**Actions Taken:**
1. Attempted `gh pr edit 35 --add-reviewer copilot --repo chronicles-net/chronicles`
2. Verified via `gh api users/copilot` → HTTP 404 Not Found
3. Confirmed: GitHub has no user account named "copilot"

**Finding:** GitHub's Copilot AI review feature is not assignable as a traditional PR reviewer username. It is a bot service integrated at the organizational level, not a standalone GitHub account.

**Outcome:** Review assignment blocked by GitHub API validation. Recommendation: Use GitHub's organization-level Copilot settings or assign to a real team member.

## Learnings

- **GitHub User Resolution in CLI**: When requesting reviewers via `gh pr edit --add-reviewer <username>`, GitHub API validates user existence in real-time. Non-existent users return `GraphQL: Could not resolve user with login '<username>'` error. No workaround for fictional/non-existent users.
- **Conventional Commit Trailers**: The `Co-authored-by` trailer format specified in git commit conventions (RFC 2822 style) is distinct from GitHub username resolution for reviews. Trailers can reference fictional/non-existent users for co-authorship tracking, but review assignment requires real GitHub accounts.
- **GitHub Copilot Review Limitations**: GitHub's Copilot AI reviewer is not assignable as a direct PR reviewer via username. It functions as an organizational/repository-level automation, not a user account. To enable Copilot reviews, configure via organization settings or repository AI policies, not via direct `--add-reviewer` commands.
