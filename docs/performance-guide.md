# Performance Guide

This guide covers optimization techniques, scaling strategies, and performance considerations for Chronicles applications.

## 🎯 Performance Fundamentals

### Understanding Chronicles Performance Characteristics

Chronicles performance is primarily influenced by:
- **Cosmos DB performance** - RU/s allocation, partition distribution
- **Event volume** - Number of events per stream, event size
- **Projection complexity** - How many projections process each event
- **Query patterns** - Partition-scoped vs cross-partition queries

### Key Metrics to Monitor

```csharp
// Custom metrics to track
public class PerformanceMetrics
{
    public static readonly Counter EventsWritten = Metrics
        .CreateCounter("chronicles_events_written_total", "Total events written");
        
    public static readonly Histogram CommandExecutionTime = Metrics
        .CreateHistogram("chronicles_command_duration_seconds", "Command execution time");
        
    public static readonly Gauge ActiveStreams = Metrics
        .CreateGauge("chronicles_active_streams", "Number of active streams");
        
    public static readonly Counter ProjectionEvents = Metrics
        .CreateCounter("chronicles_projection_events_total", "Events processed by projections");
}
```

## 📊 Cosmos DB Optimization

### Partition Strategy

```csharp
// ✅ Good: Even distribution across partitions
public static class StreamPartitioning
{
    public static StreamId CreateOrderStream(string customerId, string orderId)
    {
        // Partition by customer for query efficiency
        return new StreamId("order", $"{customerId}-{orderId}");
    }
    
    public static StreamId CreateCustomerStream(string customerId)
    {
        return new StreamId("customer", customerId);
    }
    
    // For high-volume scenarios, use hash-based partitioning
    public static StreamId CreateSessionStream(string userId, string sessionId)
    {
        var partitionHash = Math.Abs(userId.GetHashCode()) % 1000;
        return new StreamId("session", $"p{partitionHash:000}", sessionId);
    }
}

// ❌ Avoid: Hot partitions
public static StreamId CreateGlobalStream(string eventType)
{
    return new StreamId("global", eventType); // All events in same partition
}
```

### Request Unit (RU) Optimization

```csharp
public class OptimizedDocumentWriter<T> where T : IDocument
{
    private readonly IDocumentWriter<T> _writer;

    // Batch operations to reduce RU consumption
    public async Task<List<T>> CreateBatchAsync(
        IEnumerable<T> documents, 
        string partitionKey,
        CancellationToken cancellationToken = default)
    {
        const int batchSize = 100; // Cosmos DB transaction limit
        var results = new List<T>();
        
        foreach (var batch in documents.Chunk(batchSize))
        {
            using var transaction = _writer.CreateTransaction(partitionKey);
            
            foreach (var document in batch)
            {
                await transaction.CreateAsync(document);
            }
            
            var batchResults = await transaction.ExecuteAsync();
            results.AddRange(batchResults.Select(r => r.Resource));
        }
        
        return results;
    }

    // Use appropriate consistency levels
    public async Task<T> ReadWithEventualConsistencyAsync<TResult>(
        string documentId,
        string partitionKey) where TResult : T
    {
        return await _writer.ReadAsync<TResult>(
            documentId,
            partitionKey,
            new ItemRequestOptions 
            { 
                ConsistencyLevel = ConsistencyLevel.Eventual // Lower RU cost
            });
    }
}
```

### Query Optimization

```csharp
public class OptimizedQueryService
{
    private readonly IDocumentReader<OrderView> _reader;

    // ✅ Good: Partition-scoped with appropriate indexing
    public async Task<PagedResult<OrderView>> GetCustomerOrdersOptimizedAsync(
        string customerId,
        DateTime? fromDate = null,
        int pageSize = 20,
        string? continuationToken = null)
    {
        var sql = new StringBuilder("SELECT * FROM c WHERE c.CustomerId = @customerId");
        var query = new QueryDefinition(sql.ToString())
            .WithParameter("@customerId", customerId);

        if (fromDate.HasValue)
        {
            sql.Append(" AND c.CreatedAt >= @fromDate");
            query.WithParameter("@fromDate", fromDate.Value);
        }

        sql.Append(" ORDER BY c.CreatedAt DESC");

        return await _reader.PagedQueryAsync<OrderView>(
            new QueryDefinition(sql.ToString()),
            partitionKey: customerId, // Partition-scoped
            maxItemCount: pageSize,
            continuationToken: continuationToken,
            options: new QueryRequestOptions
            {
                MaxItemCount = pageSize,
                MaxConcurrency = 4 // Parallel execution within partition
            });
    }

    // Use indexing hints for complex queries
    public async Task<List<OrderView>> GetOrdersByStatusOptimizedAsync(
        string customerId,
        string status)
    {
        var query = new QueryDefinition(@"
            SELECT * FROM c 
            WHERE c.CustomerId = @customerId 
            AND c.Status = @status
            ORDER BY c.CreatedAt DESC")
            .WithParameter("@customerId", customerId)
            .WithParameter("@status", status);

        var results = new List<OrderView>();
        await foreach (var order in _reader.QueryAsync<OrderView>(
            query,
            customerId,
            options: new QueryRequestOptions
            {
                EnableLowPrecisionOrderBy = true, // Reduce RU for ORDER BY
                MaxItemCount = 100
            }))
        {
            results.Add(order);
        }

        return results;
    }
}
```

## ⚡ Event Processing Optimization

### Batch Event Processing

```csharp
public class BatchEventProcessor : IEventProcessor
{
    private readonly IDocumentWriter<ProjectionState> _writer;
    private readonly ConcurrentQueue<StreamEvent> _eventQueue = new();
    private readonly Timer _batchTimer;
    private readonly SemaphoreSlim _processingLock = new(1, 1);

    public BatchEventProcessor(IDocumentWriter<ProjectionState> writer)
    {
        _writer = writer;
        _batchTimer = new Timer(ProcessBatch, null, 
            TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    public async ValueTask ConsumeAsync(
        StreamEvent evt, 
        IStateContext state, 
        bool hasMore, 
        CancellationToken cancellationToken)
    {
        // Queue events for batch processing
        _eventQueue.Enqueue(evt);
        
        // Process immediately if batch is full
        if (_eventQueue.Count >= 100)
        {
            _ = Task.Run(() => ProcessBatch(null));
        }
    }

    private async void ProcessBatch(object? state)
    {
        if (!await _processingLock.WaitAsync(100))
            return; // Skip if already processing

        try
        {
            var events = new List<StreamEvent>();
            
            // Drain queue
            while (_eventQueue.TryDequeue(out var evt) && events.Count < 100)
            {
                events.Add(evt);
            }

            if (events.Count == 0)
                return;

            await ProcessEventBatch(events);
        }
        finally
        {
            _processingLock.Release();
        }
    }

    private async Task ProcessEventBatch(List<StreamEvent> events)
    {
        // Group events by partition for efficient processing
        var eventsByPartition = events
            .GroupBy(e => GetPartitionKey(e))
            .ToDictionary(g => g.Key, g => g.ToList());

        var tasks = eventsByPartition.Select(async kvp =>
        {
            var partitionKey = kvp.Key;
            var partitionEvents = kvp.Value;

            using var transaction = _writer.CreateTransaction(partitionKey);
            
            foreach (var evt in partitionEvents)
            {
                await ProcessSingleEvent(evt, transaction);
            }
            
            await transaction.ExecuteAsync();
        });

        await Task.WhenAll(tasks);
    }
}
```

### Async Event Processing

```csharp
public class AsyncEventProcessor : IEventProcessor
{
    private readonly IServiceBus _serviceBus;
    private readonly ILogger<AsyncEventProcessor> _logger;

    public async ValueTask ConsumeAsync(
        StreamEvent evt, 
        IStateContext state, 
        bool hasMore, 
        CancellationToken cancellationToken)
    {
        // Process critical events synchronously
        if (IsCriticalEvent(evt))
        {
            await ProcessImmediately(evt, cancellationToken);
            return;
        }

        // Queue non-critical events for async processing
        await _serviceBus.PublishAsync(new EventProcessingMessage
        {
            StreamId = evt.Metadata.StreamId.ToString(),
            EventType = evt.Metadata.Name,
            EventData = JsonSerializer.Serialize(evt.Data),
            Metadata = evt.Metadata
        }, cancellationToken);

        _logger.LogDebug("Queued {EventType} for async processing", evt.Metadata.Name);
    }

    private static bool IsCriticalEvent(StreamEvent evt)
    {
        return evt.Data switch
        {
            OrderCreated => true,
            OrderSubmitted => true,
            PaymentProcessed => true,
            _ => false
        };
    }
}
```

## 🔄 Projection Optimization

### Incremental Projections

```csharp
public class OptimizedOrderSummaryProjection : IDocumentProjection<OrderSummaryView>
{
    public OrderSummaryView CreateState(StreamId streamId)
    {
        return new OrderSummaryView { OrderId = streamId.Id };
    }

    public OrderSummaryView? ConsumeEvent(StreamEvent evt, OrderSummaryView state)
    {
        // Only update if the event affects this projection
        return evt.Data switch
        {
            OrderCreated e => state with
            {
                CustomerId = e.CustomerId,
                Status = "Created",
                CreatedAt = e.CreatedAt,
                LastUpdated = evt.Metadata.Timestamp,
                Version = evt.Metadata.Version.Value
            },
            OrderItemAdded e => state with
            {
                ItemCount = state.ItemCount + 1,
                TotalAmount = state.TotalAmount + (e.Quantity * e.UnitPrice),
                LastUpdated = evt.Metadata.Timestamp,
                Version = evt.Metadata.Version.Value
            },
            OrderSubmitted e => state with
            {
                Status = "Submitted",
                SubmittedAt = e.SubmittedAt,
                LastUpdated = evt.Metadata.Timestamp,
                Version = evt.Metadata.Version.Value
            },
            // Ignore events that don't affect this projection
            _ => null
        };
    }

    public async ValueTask<DocumentCommitAction> OnCommitAsync(
        OrderSummaryView document, 
        CancellationToken cancellationToken)
    {
        // Use optimistic concurrency for updates
        return DocumentCommitAction.Update;
    }
}
```

### Projection Rebuilding Strategies

```csharp
public class ProjectionRebuilder
{
    private readonly IEventStreamReader _eventReader;
    private readonly IDocumentWriter<OrderSummaryView> _writer;

    public async Task RebuildProjectionAsync(
        string streamId,
        StreamVersion? fromVersion = null,
        CancellationToken cancellationToken = default)
    {
        var projection = new OptimizedOrderSummaryProjection();
        var streamIdObj = StreamId.FromString(streamId);
        var state = projection.CreateState(streamIdObj);

        var readOptions = new StreamReadOptions
        {
            FromVersion = fromVersion ?? StreamVersion.Any,
            ToVersion = StreamVersion.Any
        };

        // Process events in chunks for memory efficiency
        const int chunkSize = 1000;
        var eventCount = 0;
        var events = new List<StreamEvent>();

        await foreach (var evt in _eventReader.ReadAsync(streamIdObj, readOptions, cancellationToken: cancellationToken))
        {
            events.Add(evt);
            eventCount++;

            if (events.Count >= chunkSize)
            {
                state = await ProcessEventChunk(events, state, projection);
                events.Clear();
            }
        }

        // Process remaining events
        if (events.Count > 0)
        {
            state = await ProcessEventChunk(events, state, projection);
        }

        // Save final state
        await _writer.WriteAsync(state, new ItemRequestOptions(), cancellationToken: cancellationToken);
    }

    private async Task<OrderSummaryView> ProcessEventChunk(
        List<StreamEvent> events,
        OrderSummaryView currentState,
        OptimizedOrderSummaryProjection projection)
    {
        foreach (var evt in events)
        {
            var updated = projection.ConsumeEvent(evt, currentState);
            if (updated != null)
                currentState = updated;
        }

        return currentState;
    }
}
```

## 💾 Caching Strategies

### In-Memory Caching

```csharp
public class CachedQueryService
{
    private readonly OrderQueryService _queryService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedQueryService> _logger;

    public CachedQueryService(
        OrderQueryService queryService,
        IMemoryCache cache,
        ILogger<CachedQueryService> logger)
    {
        _queryService = queryService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<OrderSummary> GetOrderSummaryAsync(string orderId)
    {
        var cacheKey = $"order-summary:{orderId}";
        
        if (_cache.TryGetValue(cacheKey, out OrderSummary cachedSummary))
        {
            _logger.LogDebug("Cache hit for order {OrderId}", orderId);
            return cachedSummary;
        }

        var summary = await _queryService.GetOrderSummaryAsync(orderId);
        
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
            SlidingExpiration = TimeSpan.FromMinutes(1),
            Priority = CacheItemPriority.Normal
        };

        _cache.Set(cacheKey, summary, cacheOptions);
        _logger.LogDebug("Cached order summary for {OrderId}", orderId);
        
        return summary;
    }

    // Invalidate cache when order is updated
    public void InvalidateOrderCache(string orderId)
    {
        var cacheKey = $"order-summary:{orderId}";
        _cache.Remove(cacheKey);
        _logger.LogDebug("Invalidated cache for order {OrderId}", orderId);
    }
}

// Event processor to handle cache invalidation
public class CacheInvalidationProcessor : IEventProcessor
{
    private readonly CachedQueryService _cachedQueryService;

    public async ValueTask ConsumeAsync(
        StreamEvent evt, 
        IStateContext state, 
        bool hasMore, 
        CancellationToken cancellationToken)
    {
        if (evt.Metadata.StreamId.Category == "order")
        {
            _cachedQueryService.InvalidateOrderCache(evt.Metadata.StreamId.Id);
        }
    }
}
```

### Distributed Caching

```csharp
public class DistributedCachedQueryService
{
    private readonly OrderQueryService _queryService;
    private readonly IDistributedCache _distributedCache;
    private readonly JsonSerializerOptions _jsonOptions;

    public async Task<OrderSummary> GetOrderSummaryAsync(string orderId)
    {
        var cacheKey = $"order-summary:{orderId}";
        var cachedJson = await _distributedCache.GetStringAsync(cacheKey);
        
        if (!string.IsNullOrEmpty(cachedJson))
        {
            return JsonSerializer.Deserialize<OrderSummary>(cachedJson, _jsonOptions)!;
        }

        var summary = await _queryService.GetOrderSummaryAsync(orderId);
        
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
            SlidingExpiration = TimeSpan.FromMinutes(2)
        };

        var jsonToCache = JsonSerializer.Serialize(summary, _jsonOptions);
        await _distributedCache.SetStringAsync(cacheKey, jsonToCache, options);
        
        return summary;
    }
}
```

## 📈 Scaling Patterns

### Horizontal Scaling

```csharp
// Configuration for multiple Cosmos DB regions
public void ConfigureServices(IServiceCollection services)
{
    services.AddChronicles(options =>
    {
        // Primary region for writes
        options.AddDocumentStore("primary", primaryConnectionString, storeOptions =>
        {
            storeOptions.WithConsistencyLevel(ConsistencyLevel.Session);
            storeOptions.WithApplicationRegion(Regions.EastUS);
        });
        
        // Read-only replicas for queries
        options.AddDocumentStore("read-westus", readOnlyConnectionStringWest, storeOptions =>
        {
            storeOptions.WithConsistencyLevel(ConsistencyLevel.Eventual);
            storeOptions.WithApplicationRegion(Regions.WestUS);
        });
        
        options.AddDocumentStore("read-europe", readOnlyConnectionStringEurope, storeOptions =>
        {
            storeOptions.WithConsistencyLevel(ConsistencyLevel.Eventual);
            storeOptions.WithApplicationRegion(Regions.NorthEurope);
        });
    });
}

// Service that routes queries to nearest read replica
public class GeographicallyDistributedQueryService
{
    private readonly IDocumentReader<OrderView> _primaryReader;
    private readonly IDocumentReader<OrderView> _westUsReader;
    private readonly IDocumentReader<OrderView> _europeReader;
    private readonly string _currentRegion;

    public async Task<OrderView> GetOrderAsync(string orderId, string customerId)
    {
        var reader = _currentRegion switch
        {
            "west-us" => _westUsReader,
            "europe" => _europeReader,
            _ => _primaryReader
        };

        return await reader.ReadAsync<OrderView>(orderId, customerId, null, GetStoreName(_currentRegion));
    }

    private string GetStoreName(string region) => region switch
    {
        "west-us" => "read-westus",
        "europe" => "read-europe",  
        _ => "primary"
    };
}
```

### Load Balancing Event Processing

```csharp
public class LoadBalancedEventProcessor
{
    private readonly IEventProcessor[] _processors;
    private int _currentProcessorIndex = 0;

    public LoadBalancedEventProcessor(IEventProcessor[] processors)
    {
        _processors = processors;
    }

    public async ValueTask ConsumeAsync(
        StreamEvent evt, 
        IStateContext state, 
        bool hasMore, 
        CancellationToken cancellationToken)
    {
        // Round-robin load balancing
        var processor = GetNextProcessor();
        await processor.ConsumeAsync(evt, state, hasMore, cancellationToken);
    }

    private IEventProcessor GetNextProcessor()
    {
        var index = Interlocked.Increment(ref _currentProcessorIndex) % _processors.Length;
        return _processors[index];
    }
}

// Partition-based processing for better distribution
public class PartitionBasedEventProcessor
{
    private readonly Dictionary<string, IEventProcessor> _processorsByPartition = new();

    public async ValueTask ConsumeAsync(
        StreamEvent evt, 
        IStateContext state, 
        bool hasMore, 
        CancellationToken cancellationToken)
    {
        var partitionKey = GetPartitionKey(evt);
        var processor = GetProcessorForPartition(partitionKey);
        
        await processor.ConsumeAsync(evt, state, hasMore, cancellationToken);
    }

    private string GetPartitionKey(StreamEvent evt)
    {
        // Extract partition key from stream ID or event data
        return evt.Metadata.StreamId.Category switch
        {
            "order" => $"orders_{Math.Abs(evt.Metadata.StreamId.Id.GetHashCode()) % 4}",
            "customer" => $"customers_{Math.Abs(evt.Metadata.StreamId.Id.GetHashCode()) % 4}",
            _ => "default"
        };
    }
}
```

## 📊 Performance Monitoring

### Application Insights Integration

```csharp
public class TelemetryEventProcessor : IEventProcessor
{
    private readonly TelemetryClient _telemetryClient;

    public async ValueTask ConsumeAsync(
        StreamEvent evt, 
        IStateContext state, 
        bool hasMore, 
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await ProcessEvent(evt, state, cancellationToken);
            
            // Track successful processing
            _telemetryClient.TrackEvent("EventProcessed", new Dictionary<string, string>
            {
                ["EventType"] = evt.Metadata.Name,
                ["StreamCategory"] = evt.Metadata.StreamId.Category,
                ["ProcessingTimeMs"] = stopwatch.ElapsedMilliseconds.ToString()
            });
            
            _telemetryClient.TrackMetric("EventProcessingTime", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _telemetryClient.TrackException(ex, new Dictionary<string, string>
            {
                ["EventType"] = evt.Metadata.Name,
                ["StreamId"] = evt.Metadata.StreamId.ToString()
            });
            throw;
        }
    }
}
```

### Custom Performance Counters

```csharp
public class PerformanceTrackingCommandProcessor<TCommand> : ICommandProcessor<TCommand>
    where TCommand : class
{
    private readonly ICommandProcessor<TCommand> _inner;
    private readonly IMetrics _metrics;

    public async Task<CommandResult> ExecuteAsync(
        StreamId streamId, 
        TCommand command, 
        CommandRequestOptions? requestOptions, 
        CancellationToken cancellationToken)
    {
        using var activity = Activity.StartActivity($"Command.{typeof(TCommand).Name}");
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await _inner.ExecuteAsync(streamId, command, requestOptions, cancellationToken);
            
            // Track metrics
            _metrics.Measure.Counter.Increment(
                "commands_executed_total",
                new MetricTags("command_type", typeof(TCommand).Name, "result", result.Result.ToString()));
                
            _metrics.Measure.Histogram.Update(
                "command_duration_seconds",
                stopwatch.Elapsed.TotalSeconds,
                new MetricTags("command_type", typeof(TCommand).Name));
            
            return result;
        }
        catch (Exception ex)
        {
            _metrics.Measure.Counter.Increment(
                "commands_failed_total",
                new MetricTags("command_type", typeof(TCommand).Name, "error_type", ex.GetType().Name));
            throw;
        }
    }
}
```

## 🚀 Next Steps

- **[Troubleshooting](./troubleshooting.md)** - Common performance issues and solutions
- **[Best Practices](./best-practices.md)** - General development patterns
- **[Testing Layer](./testing-layer.md)** - Performance testing strategies

---

> ⚡ **Performance Tip**: Always measure before optimizing. Use monitoring and profiling tools to identify actual bottlenecks rather than optimizing based on assumptions.