# Copilot Instructions — Chronicles

## Project Overview

Chronicles is a .NET event sourcing and CQRS library backed by Azure Cosmos DB. It provides:

- **EventStore**: Append-only event streams persisted to Cosmos DB
- **CQRS**: Command handlers (`ICommandHandler<T>`), command processors, state projections, and document projections
- **Document Store**: Read/write layer over Cosmos DB with change-feed subscriptions

Target framework: `net10.0`. SDK version pinned via `global.json` (9.0+ with `rollForward: latestMajor`, prerelease allowed).

## Branch & Commit Workflow

### Branch Rules

- **Never commit directly to `main`, `preview`, or `insider`.** Always create a feature or fix branch first.
- Trunk-based development: branches are short-lived, based off `main`.
- Pull requests target `main`. CI runs on push to `main` and on PRs to `main`.

### Commit Conventions

- Use [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/): `feat:`, `fix:`, `chore:`, `refactor:`, `test:`, `docs:`.
- **Keep commits small and logically atomic** — one concern per commit. Do not bundle unrelated changes.
- Every commit should leave the build green.

## Build & Test

```bash
dotnet build -c Release
dotnet test -c Release
```

- Test framework: **xUnit v3**
- Every bug fix or feature must include corresponding tests.
- CI pipeline (`.github/workflows/ci.yml`) runs: restore → build → test → coverage report.
- Warnings are treated as errors in `Release` configuration.

## Package Management

This repository uses **NuGet Central Package Management**. Package versions are defined in `Directory.Packages.props` at the repository root.

- **Never specify a `Version` attribute in `.csproj` files.** Only add `<PackageReference Include="..." />` — the version is resolved centrally.
- To update or add a dependency, edit `Directory.Packages.props`.

## C# Coding Conventions

The repository enforces conventions via `.editorconfig` and .NET analyzers. Key rules:

### Style

- **File-scoped namespaces**: `namespace Chronicles.EventStore;`
- **Nullable reference types**: enabled (`<Nullable>enable</Nullable>`)
- **Implicit usings**: enabled
- Prefer `var` when the type is apparent
- Prefer pattern matching over `as`/`is` with null/cast checks
- Prefer expression-bodied members where natural (accessors, properties, indexers)

### Naming

| Member | Convention | Example |
|--------|-----------|---------|
| Types, namespaces, methods, properties, events | `PascalCase` | `StreamId`, `AppendAsync` |
| Interfaces | `IPascalCase` | `ICommandHandler` |
| Type parameters | `TPascalCase` | `TState` |
| Private instance fields | `camelCase` | `containerProvider` |
| Private static fields | `camelCase` | `instance` |
| Local variables, parameters | `camelCase` | `streamId` |
| Constants (public/private) | `PascalCase` | `DefaultTimeout` |

### Formatting

- 4-space indentation (no tabs)
- Allman-style braces (opening brace on new line)
- Sort `System` usings first
- Insert final newline

### Analyzers

- .NET analyzers enabled at `latest-recommended` analysis level
- Code style is enforced in build (`EnforceCodeStyleInBuild`)
- Warnings are errors in Release builds — do not suppress warnings without justification

## Architecture Patterns

### Event Sourcing

Events are the source of truth, stored as an append-only stream in Cosmos DB. Each stream is identified by a `StreamId` (category + aggregate ID).

### CQRS

- **Commands** flow through `ICommandProcessor` → `ICommandHandler<TCommand>` → event store
- **State projections** (`IStateProjection<TState>`) rebuild aggregate state by folding events
- **Document projections** (`IDocumentProjection<TDocument>`) build read models from events via change-feed subscriptions
- Command handlers produce events; they do not mutate state directly

### Key Interfaces

- `ICommandHandler<TCommand>` — handles a command, produces events
- `IStatelessCommandHandler<TCommand>` — command handler without state dependency
- `IStateProjection<TState>` — folds events into aggregate state
- `IDocumentProjection<TDocument>` — projects events into a read-model document
- `IEventProcessor` — processes events from the change feed

## AI Team State — Protected Branches

The Squad AI team framework commits coordination files (logs, decisions, agent state) to dev and feature branches as part of its workflow. These are internal working artifacts that must never reach production branches.

The following paths are blocked from `main`, `preview`, and `insider` by the `squad-main-guard.yml` workflow:

- `.squad/` — AI team runtime state (decisions, logs, agent history)
- `.ai-team/` — legacy AI team state
- `.ai-team-templates/` — Squad's internal planning templates
- `team-docs/` — internal team documentation
- `docs/proposals/` — design proposals (draft/internal)

These files live on dev branches by design. Use `git rm --cached -r <path>` to untrack them from a PR if the guard blocks your merge.
