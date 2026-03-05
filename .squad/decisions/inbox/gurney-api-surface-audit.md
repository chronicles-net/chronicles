# Public API Surface Audit — Chronicles v1.0.0

**Auditor:** Gurney (Backend Dev)  
**Date:** 2026-03-04  
**Requested by:** Lars Skovslund  

## Summary

Comprehensive audit of the public API surface for Chronicles prior to v1.0.0 release. The audit examined all `.cs` files in `src/Chronicles/` to verify that implementation details are properly marked `internal` and that the intentional public API is clean and consistent.

## InternalsVisibleTo Status

✅ **CONFIGURED** — `InternalsVisibleTo` is properly set in `IsExternalInit.cs`:
- `Chronicles.Tests` (test project access)
- `DynamicProxyGenAssembly2` (for mocking frameworks)

## Findings

### 1. Should Be Internal — Currently Public

Only **ONE** type needs to be changed:

| Type | File Path | Current | Proposed | Reason |
|------|-----------|---------|----------|--------|
| `EventDocumentBase` | `src/Chronicles/EventStore/Internal/EventDocumentBase.cs` | `public abstract record` | `internal abstract record` | Implementation detail in `Internal` namespace. Base class for internal Cosmos DB document mappings (`EventDocument`, `StreamMetadataDocument`). Not intended for consumer use. |

### 2. Confirmed Public API

The following categories represent the intentional, well-designed public API surface:

#### 2.1 Core Interfaces (Event Store)

**File:** `src/Chronicles/EventStore/`

| Interface | Purpose |
|-----------|---------|
| `IEventCatalog` | Event type registration and discovery |
| `IEventDataConverter` | Custom event serialization |
| `IEventProcessor` | Process events from change feed |
| `IEventStreamProcessor` | Process entire event streams |
| `IEventStreamReader` | Read events from streams |
| `IEventStreamWriter` | Write events to streams |
| `IEventSubscriptionExceptionHandler` | Handle subscription errors |
| `IStateContext` | Access to current state during event processing |

#### 2.2 Core Interfaces (CQRS)

**File:** `src/Chronicles/Cqrs/`

| Interface | Purpose |
|-----------|---------|
| `ICommandHandler<TCommand, TState>` | Stateful command handler with state projection |
| `ICommandHandler<TCommand>` | Stateful command handler (infers state from attribute) |
| `IStatelessCommandHandler<TCommand>` | Stateless command handler |
| `ICommandContext<TCommand>` | Command execution context (event emission) |
| `ICommandContext<TCommand, TState>` | Command execution context with state access |
| `ICommandCompletionContext<TCommand>` | Post-execution context for completion handlers |
| `ICommandExecutor<TCommand>` | Execute commands programmatically |
| `ICommandProcessor<TCommand>` | Process commands (lower-level than executor) |
| `ICommandProcessorFactory` | Create command processors |
| `IStateProjection<TState>` | Fold events into aggregate state |
| `IStateConsumer<TState>` | Consume state changes (read-side) |
| `IDocumentProjection<TDocument>` | Project events into read-model documents |
| `IDocumentProjectionRebuilder<TProjection, TDocument>` | Rebuild document projections |
| `IDocumentPublisher<TDocument>` | Publish projected documents |

#### 2.3 Core Interfaces (Documents)

**File:** `src/Chronicles/Documents/`

| Interface | Purpose |
|-----------|---------|
| `IDocument` | Document identity and partitioning |
| `IDocumentReader<T>` | Read documents from Cosmos DB |
| `IDocumentWriter<T>` | Write documents to Cosmos DB |
| `IDocumentTransaction<T>` | Transactional batch operations |
| `IDocumentProcessor<T>` | Process document changes from change feed |
| `IContainerInitializer` | Initialize Cosmos DB containers |
| `ISubscriptionService` | Manage change feed subscriptions |

#### 2.4 Value Types & Records

**File:** `src/Chronicles/EventStore/`

| Type | Kind | Purpose |
|------|------|---------|
| `StreamId` | record | Identifies an event stream (category + aggregate ID) |
| `StreamVersion` | struct | Stream version/position (optimistic concurrency) |
| `StreamEvent` | record | Event with metadata from stream |
| `StreamMetadata` | abstract record | Stream state, version, timestamp |
| `EventMetadata` | record | Event metadata (causation, correlation) |
| `Checkpoint` | record | Change feed checkpoint |
| `Checkpoint<TState>` | record | Checkpoint with state snapshot |
| `StreamWriteResult` | record | Result of stream write operation |
| `UnknownEvent` | record | Event with unknown type (deserialization fallback) |
| `FaultedEvent` | record | Event that failed to deserialize |
| `EventConverterContext` | record | Context for event conversion |

#### 2.5 Configuration Classes

**File:** `src/Chronicles/EventStore/`, `src/Chronicles/Cqrs/`, `src/Chronicles/Documents/`

| Type | Purpose |
|------|---------|
| `EventStoreOptions` | Configure event store (container names, serialization) |
| `EventSubscriptionOptions` | Configure change feed subscriptions |
| `StreamOptions` | Base class for stream operation options |
| `StreamReadOptions` | Options for reading streams |
| `StreamWriteOptions` | Options for writing streams |
| `CommandOptions` | Command execution options (consistency, conflict behavior) |
| `CommandRequestOptions` | Per-command request options |
| `DocumentOptions` | Configure document store (connection, throughput) |
| `InitializationOptions` | Container initialization options |
| `SubscriptionOptions` | Document change feed subscription options |

#### 2.6 Result Types & Delegates

| Type | Kind | Purpose |
|------|------|---------|
| `CommandResult` | record | Result of command execution |
| `PagedResult<T>` | class | Paginated query results |
| `CommandCompletedAsync<TCommand>` | delegate | Async completion callback |
| `QueryExpression<TSource, TResult>` | delegate | LINQ query expression |

#### 2.7 Enumerations

| Enum | Purpose |
|------|---------|
| `StreamState` | Stream state (Active, Closed, Archived, Deleted) |
| `ResultType` | Stream operation result type |
| `CommandConsistency` | Command consistency level (Eventual, Strong) |
| `CommandConflictBehavior` | Command conflict resolution (Retry, Fail) |
| `DocumentCommitAction` | Document projection commit action |
| `SubscriptionStartOptions` | Change feed start position |
| `FakeDocumentAction` | Testing: document operation type |

#### 2.8 Exceptions

| Exception | Purpose |
|-----------|---------|
| `StreamConflictException` | Optimistic concurrency violation |

#### 2.9 Extension Methods

| Class | File | Purpose |
|-------|------|---------|
| `CommandContextExtensions` | `src/Chronicles/Cqrs/CommandContextExtensions.cs` | Builder pattern for command context (AddEvent, RespondWith) |
| `StreamMetadataExtensions` | `src/Chronicles/EventStore/StreamMetadataExtensions.cs` | Validate stream metadata constraints |
| `DocumentReaderExtensions` | `src/Chronicles/Documents/DocumentReaderExtensions.cs` | Convenience methods for document reads |
| `DocumentWriterExtensions` | `src/Chronicles/Documents/DocumentWriterExtensions.cs` | Convenience methods for document writes |

#### 2.10 Dependency Injection

**Builders** (fluent API for DI registration):

| Class | File | Purpose |
|-------|------|---------|
| `ChroniclesBuilder` | `src/Chronicles/Documents/DependencyInjection/` | Root builder for Chronicles |
| `DocumentStoreBuilder` | `src/Chronicles/Documents/DependencyInjection/` | Configure document stores |
| `EventStoreBuilder` | `src/Chronicles/EventStore/DependencyInjection/` | Configure event store |
| `EventProcessorBuilder` | `src/Chronicles/EventStore/DependencyInjection/` | Configure event processors |
| `EventSubscriptionBuilder` | `src/Chronicles/EventStore/DependencyInjection/` | Configure event subscriptions |
| `StreamProcessorBuilder` | `src/Chronicles/EventStore/DependencyInjection/` | Configure stream processors |
| `CqrsBuilder` | `src/Chronicles/Cqrs/DependencyInjection/` | Configure CQRS components |

**Extension Methods** (service collection extensions):

| Class | Purpose |
|-------|---------|
| `ServiceCollectionExtensions` (Documents) | `AddChronicles()` entrypoint |
| `EventStoreServiceCollectionExtensions` | `AddEventStore()`, `AddEventSubscription()` |
| `BuilderExtensions` (Cqrs) | `AddCommandHandler()`, `AddStateProjection()`, `AddDocumentProjection()` |
| `EventProcessorBuilderExtensions` (Cqrs) | Register CQRS processors |
| `InitializationOptionsExtensions` | Configure container initialization |

#### 2.11 Testing Helpers

**File:** `src/Chronicles/Testing/`

All types in this namespace are intentionally public (consumer testing support):

| Type | Purpose |
|------|---------|
| `FakeDocumentStore` | In-memory document store for testing |
| `FakeDocumentReader<T>` | In-memory document reader |
| `FakeDocumentWriter<T>` | In-memory document writer |
| `FakeDocumentTransaction<T>` | In-memory transaction |
| `FakeDocumentStoreInitializer` | In-memory initializer |
| `FakeChangeFeedFactory` | In-memory change feed factory |
| `FakeChangeFeedProcessor<T>` | In-memory change feed processor |
| `FakeContainer` | In-memory container |
| `FakePartition` | In-memory partition |
| `FakePartitionTransaction` | In-memory partition transaction |
| `FakeContainerNameRegistry` | In-memory container registry |
| `FakeQueryDefinition<T>` | In-memory query definition |
| `FakeTransactionalBatchResponse` | In-memory batch response |
| `FakeTransactionalBatchOperationResult<T>` | In-memory operation result |
| `FakeDocumentOperation` | Record of document operation |
| `FakeDocumentAction` | Enum: operation type |
| `IFakePartitionChangeTracking` | Interface for partition change tracking |
| `IFakeDocumentStoreProvider` | Interface for accessing fake document stores |

**DI Extension:**
- `ServiceCollectionForTestingExtensions` — `UseInMemory()` method to wire up fake implementations

#### 2.12 Base Classes

| Class | File | Purpose |
|-------|------|---------|
| `Document` | `src/Chronicles/Documents/Document.cs` | Base class for document models (provides IDocument implementation) |
| `DocumentPartitionProcessor<T>` | `src/Chronicles/Documents/DocumentPartitionProcessor.cs` | Base class for partition-parallel document processors |

#### 2.13 Attributes

| Attribute | Purpose |
|-----------|---------|
| `ContainerNameAttribute` | Specify Cosmos DB container name for a document type |

### 3. Internal Implementation (Correctly Marked)

The following are correctly marked as `internal`:

#### 3.1 Cqrs/Internal

All classes/interfaces in `src/Chronicles/Cqrs/Internal/`:
- `CommandContext<TCommand>`
- `CommandCompletionContext<TCommand>`
- `CommandProcessor<TCommand>`
- `CommandProcessorFactory`
- `CommandExecutorFactory`
- `StatefulCommandExecutor<...>`
- `StatelessCommandExecutor<...>`
- `CommandHandlerExecutor<...>`
- `DocumentProjectionRebuilder<...>`
- `ICommandExecutorFactory`
- All event processors in `EventProcessors/` subdirectory

#### 3.2 EventStore/Internal

All classes/interfaces in `src/Chronicles/EventStore/Internal/`:
- Document models: `EventDocument`, `CheckpointDocument`, `StreamMetadataDocument`
- Readers/Writers: `EventStreamReader`, `EventStreamWriter`, `EventDocumentReader`, `EventDocumentWriter`, `CheckpointReader`, `CheckpointWriter`, `StreamMetadataReader`
- Processors: `EventStreamProcessor`, `EventDocumentProcessor`, `EventDocumentBatchProducer`
- Converters: `EventDataConverter`, `StreamEventConverter`, `StreamIdJsonConverter`, `StreamVersionJsonConverter`
- Factories: `EventCatalogFactory`
- Helpers: `StateContext`, `JsonPropertyNames`, `EventStreamExtensions`
- All internal interfaces: `ICheckpointReader`, `ICheckpointWriter`, `IEventCatalogFactory`, `IEventDocumentReader`, `IEventDocumentWriter`, `IEventDocumentBatchProducer`, `IStreamMetadataReader`

#### 3.3 Documents/Internal

All classes/interfaces in `src/Chronicles/Documents/Internal/`:
- Cosmos wrappers: `CosmosClientProvider`, `CosmosContainerProvider`, `CosmosReader`, `CosmosWriter`, `CosmosTransaction`, `CosmosLinqQuery`
- Document store: `DocumentStore`, `DocumentStoreInitializer`, `DocumentStoreService`, `DocumentInitializer`
- Subscriptions: `SubscriptionService`, `SubscriptionInitializer`, `DocumentSubscription`
- Registries: `ContainerNameRegistry`, `DocumentTypeKey`
- Factories: `ChangeFeedFactory`
- Extensions: `FeedIteratorExtensions`, `FeedResponseExtensions`, `ItemResponseExtensions`
- All internal interfaces: `IDocumentStore`, `IDocumentStoreInitializer`, `IDocumentSubscription`, `IChangeFeedFactory`, `IContainerNameRegistry`, `ICosmosClientProvider`, `ICosmosContainerProvider`, `ICosmosLinqQuery`

#### 3.4 Root-Level Helpers

- `Arguments` (src/Chronicles/Arguments.cs) — Argument validation utilities

#### 3.5 DependencyInjection/Internal

- `ConfigureSubscriptionOptions` (EventStore)
- `EventStoreConfigureDocumentStore` (EventStore)
- `FakeDocumentStoreProvider` implementation class (Testing — interface is public)

## Recommendations

### Immediate Action (v1.0.0 Blocker)

1. **Change `EventDocumentBase` from `public` to `internal`** — This is the only type that needs correction before release.

### Future Considerations (Post-v1.0.0)

None identified. The public API surface is clean, well-organized, and follows .NET conventions:
- Interfaces are properly segregated (ISP)
- Implementation details are internal
- Extension methods provide discoverability
- Builder pattern for DI is intuitive
- Testing helpers are appropriately public

## Verification Notes

- All `Internal/` subdirectories were scanned for public types — **none found** (except the one issue above)
- All DI builder classes are correctly public (they are the configuration API)
- All extension method classes are correctly public (they are the fluent API)
- All Testing namespace types are correctly public (they are consumer testing utilities)
- `InternalsVisibleTo` is properly configured for test access

## Sign-Off

**Audit complete.** One issue found. Code change proposal to follow after Thufir review.

— Gurney, Backend Dev  
2026-03-04
