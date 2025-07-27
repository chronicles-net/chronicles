# Troubleshooting

This guide helps you diagnose and resolve common issues when working with Chronicles.

## 🚨 Common Issues

### Configuration Problems

#### Problem: "No command handler registered for command type"

```
InvalidOperationException: No command handler registered for command type 'OrderSystem.Commands.CreateOrder'
```

**Solution:**
```csharp
// Ensure command handler is registered in Program.cs
services.AddChronicles(options =>
{
    options.AddCqrs(cqrs =>
    {
        // Missing registration - add this line
        cqrs.AddCommandHandler<CreateOrder, CreateOrderHandler>();
    });
});
```

#### Problem: "Document store not found"

```
ArgumentException: Document store 'main' not found
```

**Solution:**
```csharp
// Ensure document store is configured
services.AddChronicles(options =>
{
    // Add this configuration
    options.AddDocumentStore("main", connectionString);
});
```

#### Problem: "Connection string issues"

```
CosmosException: Unauthorized (401) - Invalid authentication token
```

**Solutions:**
```csharp
// 1. Check connection string format
var connectionString = "AccountEndpoint=https://your-account.documents.azure.com:443/;AccountKey=your-key==;";

// 2. For Cosmos DB Emulator
var emulatorConnectionString = "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

// 3. Verify emulator is running
// Docker: docker run -p 8081:8081 mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator

// 4. Check firewall and network connectivity
```

### Event Store Issues

#### Problem: "Stream conflict when writing events"

```csharp
StreamConflictException: Expected version 5 but stream is at version 7
```

**Solution:**
```csharp
// Handle optimistic concurrency properly
public async Task<CommandResult> UpdateOrderAsync(string orderId, UpdateOrder command)
{
    const int maxRetries = 3;
    var retryCount = 0;
    
    while (retryCount < maxRetries)
    {
        try
        {
            var streamId = new StreamId("order", orderId);
            var metadata = await _eventReader.GetMetadataAsync(streamId);
            
            return await _commandProcessor.ExecuteAsync(
                streamId,
                command,
                new CommandRequestOptions 
                { 
                    ExpectedVersion = metadata.Version 
                },
                CancellationToken.None);
        }
        catch (StreamConflictException)
        {
            retryCount++;
            if (retryCount >= maxRetries)
                throw;
                
            // Wait before retry with exponential backoff
            await Task.Delay(TimeSpan.FromMilliseconds(100 * Math.Pow(2, retryCount)));
        }
    }
    
    throw new InvalidOperationException("Max retries exceeded");
}
```

#### Problem: "Events not appearing in projections"

**Diagnostic Steps:**
```csharp
// 1. Verify events are being written
public async Task DiagnoseEventWriting()
{
    var streamId = new StreamId("order", "test-order");
    var events = new List<object> { new OrderCreated("test-order", "customer-123", DateTime.UtcNow) };
    
    var result = await _eventWriter.WriteAsync(streamId, events.ToImmutableList());
    Console.WriteLine($"Written to version: {result.Version}");
    
    // 2. Verify events can be read back
    await foreach (var evt in _eventReader.ReadAsync(streamId))
    {
        Console.WriteLine($"Event: {evt.Data.GetType().Name} at version {evt.Metadata.Version}");
    }
}

// 3. Check projection registration
services.AddChronicles(options =>
{
    options.AddCqrs(cqrs =>
    {
        // Ensure projection is registered
        cqrs.AddProjection<OrderSummaryProjection, OrderSummary>();
    });
});

// 4. Add logging to projection
public class OrderSummaryProjection : IStateProjection<OrderSummary>
{
    private readonly ILogger<OrderSummaryProjection> _logger;

    public OrderSummary? ConsumeEvent(StreamEvent evt, OrderSummary state)
    {
        _logger.LogInformation("Processing event {EventType} for stream {StreamId}", 
            evt.Data.GetType().Name, evt.Metadata.StreamId);
            
        return evt.Data switch
        {
            OrderCreated e => UpdateForOrderCreated(state, e),
            _ => null
        };
    }
}
```

### Serialization Issues

#### Problem: "Event deserialization fails"

```
JsonException: The JSON value could not be converted to OrderSystem.Events.OrderCreated
```

**Solutions:**
```csharp
// 1. Ensure events are serializable
public record OrderCreated(
    string OrderId,
    string CustomerId,
    DateTime CreatedAt)  // All properties must be serializable
{
    // Avoid: Complex nested objects, circular references
    // Avoid: Properties without public setters in older .NET versions
}

// 2. Configure custom serialization if needed
services.AddChronicles(options =>
{
    options.AddEventStore("main", eventStore =>
    {
        eventStore.ConfigureJsonSerialization(settings =>
        {
            settings.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            settings.Converters.Add(new JsonStringEnumConverter());
            settings.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });
    });
});

// 3. Handle schema evolution
public OrderSummary? ConsumeEvent(StreamEvent evt, OrderSummary state)
{
    try
    {
        return evt.Data switch
        {
            OrderCreated e => ProjectOrderCreated(state, e),
            _ => null
        };
    }
    catch (JsonException ex)
    {
        _logger.LogWarning(ex, "Failed to deserialize event {EventType} from {StreamId}", 
            evt.Metadata.Name, evt.Metadata.StreamId);
        return null; // Skip malformed events
    }
}
```

### Performance Issues

#### Problem: "Slow query performance"

**Diagnostic:**
```csharp
// Enable Cosmos DB request diagnostics
public class DiagnosticDocumentReader<T> : IDocumentReader<T> where T : IDocument
{
    private readonly IDocumentReader<T> _inner;
    private readonly ILogger<DiagnosticDocumentReader<T>> _logger;

    public async Task<TResult> ReadAsync<TResult>(
        string documentId, 
        string partitionKey, 
        ItemRequestOptions? options,
        string? storeName = null,
        CancellationToken cancellationToken = default) where TResult : T
    {
        var stopwatch = Stopwatch.StartNew();
        
        var requestOptions = options ?? new ItemRequestOptions();
        requestOptions.EnableContentResponseOnWrite = true;
        
        try
        {
            var result = await _inner.ReadAsync<TResult>(documentId, partitionKey, requestOptions, storeName, cancellationToken);
            
            _logger.LogInformation("Read document {DocumentId} in {ElapsedMs}ms, RU: {RequestCharge}",
                documentId, stopwatch.ElapsedMilliseconds, "RU info from response headers");
                
            return result;
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Failed to read document {DocumentId}, RU: {RequestCharge}",
                documentId, ex.RequestCharge);
            throw;
        }
    }
}
```

**Solutions:**
```csharp
// 1. Use appropriate indexing
public async Task<List<OrderView>> GetOrdersOptimizedAsync(string customerId)
{
    // ✅ Good: Uses partition key and indexed properties
    var query = new QueryDefinition(@"
        SELECT * FROM c 
        WHERE c.CustomerId = @customerId 
        AND c.Status = @status
        ORDER BY c.CreatedAt DESC")
        .WithParameter("@customerId", customerId)
        .WithParameter("@status", "Active");

    return await ExecuteQueryAsync(query, customerId);
}

// 2. Avoid expensive operations
public async Task<List<OrderView>> GetOrdersExpensiveAsync()
{
    // ❌ Avoid: Cross-partition queries, complex aggregations
    var query = new QueryDefinition(@"
        SELECT COUNT(*) as TotalOrders, AVG(c.TotalAmount) as AverageAmount
        FROM c 
        WHERE c.Status != 'Cancelled'
        GROUP BY c.CustomerId");

    return await ExecuteQueryAsync(query, null); // Cross-partition
}

// 3. Use pagination for large result sets
public async Task<PagedResult<OrderView>> GetOrdersPagedAsync(
    string customerId, 
    int pageSize = 50,
    string? continuationToken = null)
{
    var query = new QueryDefinition("SELECT * FROM c WHERE c.CustomerId = @customerId")
        .WithParameter("@customerId", customerId);

    return await _reader.PagedQueryAsync<OrderView>(
        query, 
        customerId, 
        pageSize, 
        continuationToken);
}
```

## 🔍 Debugging Techniques

### Enable Detailed Logging

```csharp
// In Program.cs or appsettings.json
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Chronicles": "Debug",
      "Chronicles.EventStore": "Trace",
      "Chronicles.Cqrs": "Debug",
      "Microsoft.Azure.Cosmos": "Information"
    }
  }
}
```

### Event Store Diagnostics

```csharp
public class EventStoreDiagnostics
{
    private readonly IEventStreamReader _reader;
    private readonly IEventStreamWriter _writer;

    public async Task DiagnoseStreamAsync(string streamId)
    {
        var stream = StreamId.FromString(streamId);
        
        // Check stream metadata
        try
        {
            var metadata = await _reader.GetMetadataAsync(stream);
            Console.WriteLine($"Stream: {metadata.StreamId}");
            Console.WriteLine($"State: {metadata.State}");
            Console.WriteLine($"Version: {metadata.Version.Value}");
            Console.WriteLine($"Last Updated: {metadata.Timestamp}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to read metadata: {ex.Message}");
            return;
        }

        // Read all events
        Console.WriteLine("\nEvents:");
        await foreach (var evt in _reader.ReadAsync(stream))
        {
            Console.WriteLine($"  Version {evt.Metadata.Version.Value}: {evt.Data.GetType().Name}");
            Console.WriteLine($"    Timestamp: {evt.Metadata.Timestamp}");
            Console.WriteLine($"    Correlation: {evt.Metadata.CorrelationId}");
            Console.WriteLine($"    Data: {JsonSerializer.Serialize(evt.Data, new JsonSerializerOptions { WriteIndented = true })}");
        }
    }

    public async Task TestEventWritingAsync()
    {
        var testStreamId = new StreamId("test", Guid.NewGuid().ToString());
        var testEvent = new TestEvent("Hello World", DateTime.UtcNow);
        
        try
        {
            var result = await _writer.WriteAsync(testStreamId, ImmutableList.Create<object>(testEvent));
            Console.WriteLine($"✅ Successfully wrote test event to version {result.Version}");
            
            // Read it back
            await foreach (var evt in _reader.ReadAsync(testStreamId))
            {
                Console.WriteLine($"✅ Successfully read back: {evt.Data}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to write/read test event: {ex.Message}");
        }
    }
}

public record TestEvent(string Message, DateTime Timestamp);
```

### Command Processing Diagnostics

```csharp
public class CommandDiagnostics<TCommand> where TCommand : class
{
    private readonly ICommandProcessor<TCommand> _processor;

    public async Task<CommandResult> ExecuteWithDiagnosticsAsync(
        StreamId streamId,
        TCommand command)
    {
        Console.WriteLine($"Executing command: {typeof(TCommand).Name}");
        Console.WriteLine($"Stream: {streamId}");
        Console.WriteLine($"Command data: {JsonSerializer.Serialize(command, new JsonSerializerOptions { WriteIndented = true })}");

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await _processor.ExecuteAsync(streamId, command, null, CancellationToken.None);
            
            Console.WriteLine($"✅ Command completed in {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"Result: {result.Result}");
            Console.WriteLine($"Final version: {result.Version.Value}");
            
            if (result.Response != null)
            {
                Console.WriteLine($"Response: {JsonSerializer.Serialize(result.Response, new JsonSerializerOptions { WriteIndented = true })}");
            }
            
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Command failed after {stopwatch.ElapsedMilliseconds}ms: {ex.Message}");
            Console.WriteLine($"Exception type: {ex.GetType().Name}");
            throw;
        }
    }
}
```

## 🔧 Health Checks

### System Health Monitoring

```csharp
public class ChroniclesHealthCheck : IHealthCheck
{
    private readonly IEventStreamWriter _eventWriter;
    private readonly IEventStreamReader _eventReader;
    private readonly IDocumentWriter<HealthCheckDocument> _documentWriter;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        var healthData = new Dictionary<string, object>();
        var errors = new List<string>();

        // Test event store
        try
        {
            await TestEventStore(cancellationToken);
            healthData["EventStore"] = "Healthy";
        }
        catch (Exception ex)
        {
            errors.Add($"EventStore: {ex.Message}");
            healthData["EventStore"] = "Unhealthy";
        }

        // Test document store
        try
        {
            await TestDocumentStore(cancellationToken);
            healthData["DocumentStore"] = "Healthy";
        }
        catch (Exception ex)
        {
            errors.Add($"DocumentStore: {ex.Message}");
            healthData["DocumentStore"] = "Unhealthy";
        }

        healthData["CheckTime"] = DateTime.UtcNow;

        if (errors.Any())
        {
            return HealthCheckResult.Unhealthy(
                string.Join("; ", errors), 
                data: healthData);
        }

        return HealthCheckResult.Healthy("All Chronicles components are healthy", healthData);
    }

    private async Task TestEventStore(CancellationToken cancellationToken)
    {
        var testStreamId = new StreamId("health-check", Guid.NewGuid().ToString());
        var testEvent = new HealthCheckEvent(DateTime.UtcNow);

        // Write event
        var writeResult = await _eventWriter.WriteAsync(
            testStreamId, 
            ImmutableList.Create<object>(testEvent),
            cancellationToken: cancellationToken);

        // Read event back
        await foreach (var evt in _reader.ReadAsync(testStreamId, cancellationToken: cancellationToken))
        {
            if (evt.Data is HealthCheckEvent)
                return; // Success
        }

        throw new InvalidOperationException("Failed to read back health check event");
    }

    private async Task TestDocumentStore(CancellationToken cancellationToken)
    {
        var testDoc = new HealthCheckDocument
        {
            Id = Guid.NewGuid().ToString(),
            PartitionKey = "health-check",
            Timestamp = DateTime.UtcNow
        };

        await _documentWriter.CreateAsync(testDoc, new ItemRequestOptions(), cancellationToken: cancellationToken);
        await _documentWriter.DeleteAsync(testDoc.Id, testDoc.PartitionKey, new ItemRequestOptions(), cancellationToken: cancellationToken);
    }
}

public record HealthCheckEvent(DateTime Timestamp);

public class HealthCheckDocument : Document
{
    public string Id { get; set; } = string.Empty;
    public string PartitionKey { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }

    protected override string GetDocumentId() => Id;
    protected override string GetPartitionKey() => PartitionKey;
}

// Register health check
services.AddHealthChecks()
    .AddCheck<ChroniclesHealthCheck>("chronicles");
```

## 📋 Best Practices for Troubleshooting

### 1. Structured Logging

```csharp
public class StructuredLoggingEventProcessor : IEventProcessor
{
    private readonly ILogger<StructuredLoggingEventProcessor> _logger;

    public async ValueTask ConsumeAsync(
        StreamEvent evt, 
        IStateContext state, 
        bool hasMore, 
        CancellationToken cancellationToken)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["StreamId"] = evt.Metadata.StreamId.ToString(),
            ["EventType"] = evt.Metadata.Name,
            ["EventVersion"] = evt.Metadata.Version.Value,
            ["CorrelationId"] = evt.Metadata.CorrelationId ?? "none",
            ["CausationId"] = evt.Metadata.CausationId ?? "none"
        });

        _logger.LogInformation("Processing event {EventType} for stream {StreamId}", 
            evt.Metadata.Name, evt.Metadata.StreamId);

        try
        {
            await ProcessEvent(evt, state, cancellationToken);
            _logger.LogInformation("Successfully processed event");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process event {EventType}", evt.Metadata.Name);
            throw;
        }
    }
}
```

### 2. Correlation Tracking

```csharp
public class CorrelationTrackingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationTrackingMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() 
                           ?? Guid.NewGuid().ToString();

        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers.Add("X-Correlation-ID", correlationId);

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["RequestPath"] = context.Request.Path
        });

        _logger.LogInformation("Request started");

        try
        {
            await _next(context);
            _logger.LogInformation("Request completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Request failed");
            throw;
        }
    }
}

// Use correlation ID in command processing
public class CorrelationAwareOrderService
{
    private readonly ICommandProcessor<CreateOrder> _processor;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public async Task<CommandResult> CreateOrderAsync(string orderId, string customerId)
    {
        var correlationId = _httpContextAccessor.HttpContext?.Items["CorrelationId"]?.ToString();
        
        return await _processor.ExecuteAsync(
            new StreamId("order", orderId),
            new CreateOrder(orderId, customerId),
            new CommandRequestOptions { CorrelationId = correlationId },
            CancellationToken.None);
    }
}
```

### 3. Error Recovery Patterns

```csharp
public class ResilientEventProcessor : IEventProcessor
{
    private readonly IEventProcessor _inner;
    private readonly ILogger<ResilientEventProcessor> _logger;

    public async ValueTask ConsumeAsync(
        StreamEvent evt, 
        IStateContext state, 
        bool hasMore, 
        CancellationToken cancellationToken)
    {
        const int maxRetries = 3;
        var retryCount = 0;

        while (retryCount <= maxRetries)
        {
            try
            {
                await _inner.ConsumeAsync(evt, state, hasMore, cancellationToken);
                return;
            }
            catch (Exception ex) when (IsRetriableException(ex) && retryCount < maxRetries)
            {
                retryCount++;
                var delay = TimeSpan.FromMilliseconds(100 * Math.Pow(2, retryCount));
                
                _logger.LogWarning(ex, 
                    "Event processing failed, retrying in {DelayMs}ms (attempt {Attempt}/{MaxAttempts})",
                    delay.TotalMilliseconds, retryCount, maxRetries);

                await Task.Delay(delay, cancellationToken);
            }
        }

        // Final attempt without retry
        await _inner.ConsumeAsync(evt, state, hasMore, cancellationToken);
    }

    private static bool IsRetriableException(Exception ex) =>
        ex is HttpRequestException or SocketException or TimeoutException;
}
```

## 🆘 When to Seek Help

If you're still experiencing issues after following this guide:

1. **Check the Chronicles GitHub Issues**: https://github.com/chronicles-net/chronicles/issues
2. **Review Cosmos DB documentation**: https://docs.microsoft.com/en-us/azure/cosmos-db/
3. **Enable detailed logging** and collect diagnostic information
4. **Create a minimal reproduction** of the issue
5. **Open a GitHub issue** with:
   - Chronicles version
   - .NET version
   - Minimal code reproduction
   - Error messages and stack traces
   - Relevant configuration

---

> 🔧 **Pro Tip**: When reporting issues, always include the Chronicles version, your configuration setup, and a minimal code example that reproduces the problem. This helps maintainers diagnose and fix issues faster.