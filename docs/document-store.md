# Document Store

The Chronicles document store provides read and write access to Cosmos DB documents. Use it for querying projected read models and writing documents outside the event sourcing flow.

## Core Interfaces

- `IDocumentReader<T>`: Read and query documents
- `IDocumentWriter<T>`: Write, update, and delete documents

Both interfaces are injected by type:

```csharp
public class OrderController
{
    private readonly IDocumentReader<OrderDocument> _reader;
    private readonly IDocumentWriter<OrderDocument> _writer;

    public OrderController(
        IDocumentReader<OrderDocument> reader,
        IDocumentWriter<OrderDocument> writer)
    {
        _reader = reader;
        _writer = writer;
    }
}
```

## Defining Documents

Documents must implement `IDocument`:

```csharp
using Chronicles.Documents;
using System.Text.Json.Serialization;

[ContainerName("orders")]
public record OrderDocument(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("pk")] string PartitionKey,
    string CustomerId,
    decimal TotalAmount,
    OrderStatus Status,
    DateTimeOffset PlacedAt) : IDocument
{
    public string GetDocumentId() => Id;
    public string GetPartitionKey() => PartitionKey;
}
```

## IDocumentReader\<T\>

### ReadAsync

Read a single document by ID and partition key:

```csharp
var document = await _reader.ReadAsync<OrderDocument>(
    documentId: "ord-123",
    partitionKey: "ord-123",
    options: null,
    cancellationToken: cancellationToken);
```

Throws `CosmosException` with `HttpStatusCode.NotFound` if not found.

### FindAsync

Like `ReadAsync`, but returns `null` if not found:

```csharp
var document = await _reader.FindAsync(
    documentId: "ord-123",
    partitionKey: "ord-123",
    cancellationToken: cancellationToken);

if (document == null)
{
    // Document not found
}
```

### ReadManyAsync

Read multiple documents by ID and partition key:

```csharp
var ids = new[]
{
    ("ord-123", "ord-123"),
    ("ord-456", "ord-456"),
    ("ord-789", "ord-789")
};

var documents = await _reader.ReadManyAsync<OrderDocument>(
    ids,
    options: null,
    cancellationToken: cancellationToken);

foreach (var doc in documents)
{
    Console.WriteLine($"Order: {doc.Id}, Total: {doc.TotalAmount}");
}
```

### QueryAsync

Query documents using a `QueryDefinition`:

```csharp
using Microsoft.Azure.Cosmos;

var query = new QueryDefinition(
    "SELECT * FROM c WHERE c.customerId = @customerId AND c.totalAmount > @minAmount")
    .WithParameter("@customerId", "cust-123")
    .WithParameter("@minAmount", 100m);

await foreach (var doc in _reader.QueryAsync<OrderDocument>(
    query,
    partitionKey: null,
    options: null,
    cancellationToken: cancellationToken))
{
    Console.WriteLine($"Order: {doc.Id}, Amount: {doc.TotalAmount}");
}
```

### CreateQuery (LINQ)

Create a query from a LINQ expression:

```csharp
var query = _reader.CreateQuery<OrderDocument>(
    q => q.Where(o => o.CustomerId == "cust-123" && o.TotalAmount > 100m));

await foreach (var doc in _reader.QueryAsync<OrderDocument>(
    query,
    partitionKey: null,
    options: null,
    cancellationToken: cancellationToken))
{
    Console.WriteLine($"Order: {doc.Id}");
}
```

### PagedQueryAsync

Query with pagination support:

```csharp
string? continuationToken = null;
var pageSize = 20;

do
{
    var page = await _reader.PagedQueryAsync<OrderDocument>(
        query: new QueryDefinition("SELECT * FROM c WHERE c.customerId = @customerId")
            .WithParameter("@customerId", "cust-123"),
        partitionKey: null,
        maxItemCount: pageSize,
        continuationToken: continuationToken,
        options: null,
        cancellationToken: cancellationToken);

    foreach (var doc in page.Items)
    {
        Console.WriteLine($"Order: {doc.Id}");
    }

    continuationToken = page.ContinuationToken;

} while (continuationToken != null);
```

**PagedResult\<T\>:**

```csharp
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public string? ContinuationToken { get; }
    public int Count { get; }
    public bool HasMoreResults { get; }
}
```

## IDocumentWriter\<T\>

### WriteAsync (Upsert)

Write a document (upsert: creates or replaces):

```csharp
var document = new OrderDocument(
    Id: "ord-123",
    PartitionKey: "ord-123",
    CustomerId: "cust-456",
    TotalAmount: 99.99m,
    Status: OrderStatus.Placed,
    PlacedAt: DateTimeOffset.UtcNow);

var written = await _writer.WriteAsync(
    document,
    cancellationToken: cancellationToken);
```

### CreateAsync

Create a new document (throws `CosmosException` with `HttpStatusCode.Conflict` if already exists):

```csharp
var document = new OrderDocument(
    Id: "ord-123",
    PartitionKey: "ord-123",
    CustomerId: "cust-456",
    TotalAmount: 99.99m,
    Status: OrderStatus.Placed,
    PlacedAt: DateTimeOffset.UtcNow);

await _writer.CreateAsync(
    document,
    cancellationToken: cancellationToken);
```

### ReplaceAsync

Replace an existing document (throws `CosmosException` with `HttpStatusCode.NotFound` if not found):

```csharp
var updated = document with { Status = OrderStatus.Shipped };

await _writer.ReplaceAsync(
    updated,
    cancellationToken: cancellationToken);
```

### UpdateAsync

Read-modify-write pattern with automatic retries on conflicts:

```csharp
var updated = await _writer.UpdateAsync(
    documentId: "ord-123",
    partitionKey: "ord-123",
    updateDocument: async doc =>
    {
        return doc with { Status = OrderStatus.Shipped };
    },
    retries: 3,
    cancellationToken: cancellationToken);
```

Throws `CosmosException` with `HttpStatusCode.NotFound` if not found.

### UpdateOrCreateAsync

Like `UpdateAsync`, but creates the document if it does not exist:

```csharp
var updated = await _writer.UpdateOrCreateAsync(
    getDefaultDocument: () => new OrderDocument(
        Id: "ord-123",
        PartitionKey: "ord-123",
        CustomerId: "cust-456",
        TotalAmount: 0m,
        Status: OrderStatus.Placed,
        PlacedAt: DateTimeOffset.UtcNow),
    updateDocument: async doc =>
    {
        return doc with { TotalAmount = doc.TotalAmount + 10m };
    },
    retries: 3,
    cancellationToken: cancellationToken);
```

### ConditionalUpdateAsync

Update a document only if a condition is met:

```csharp
var updated = await _writer.ConditionalUpdateAsync(
    documentId: "ord-123",
    partitionKey: "ord-123",
    condition: doc => doc.Status == OrderStatus.Placed,
    updateDocument: async doc =>
    {
        return doc with { Status = OrderStatus.Shipped };
    },
    cancellationToken: cancellationToken);

if (updated == null)
{
    // Condition not met or document changed
}
```

Returns `null` if condition is not met or document has changed.

### DeleteAsync

Delete a document:

```csharp
await _writer.DeleteAsync(
    documentId: "ord-123",
    partitionKey: "ord-123",
    cancellationToken: cancellationToken);
```

Throws `CosmosException` with `HttpStatusCode.NotFound` if not found.

### TryDeleteAsync

Like `DeleteAsync`, but returns `false` if not found instead of throwing:

```csharp
bool deleted = await _writer.TryDeleteAsync(
    documentId: "ord-123",
    partitionKey: "ord-123",
    cancellationToken: cancellationToken);

if (!deleted)
{
    // Document not found
}
```

### DeletePartitionAsync

Delete all documents in a partition:

```csharp
await _writer.DeletePartitionAsync(
    partitionKey: "ord-123",
    options: null,
    cancellationToken: cancellationToken);
```

## Transactions

Use `IDocumentTransaction<T>` for multi-document writes within a single partition:

```csharp
var transaction = _writer.CreateTransaction(partitionKey: "ord-123");

transaction.Create(new OrderDocument(...));
transaction.Replace(updatedDocument);
transaction.Delete("line-item-1");

await transaction.CommitAsync(cancellationToken: cancellationToken);
```

All operations succeed or fail together.

## Multiple Document Stores

Configure multiple document stores:

```csharp
builder.Services.AddChronicles()
    .AddDocumentStore("primary", store =>
    {
        store.Configure(options =>
        {
            options.UseConnectionString("primary-connection-string");
            options.UseDatabase("primary-database");
        });
    })
    .AddDocumentStore("archive", store =>
    {
        store.Configure(options =>
        {
            options.UseConnectionString("archive-connection-string");
            options.UseDatabase("archive-database");
        });
    });
```

Specify the store name in read/write operations:

```csharp
var document = await _reader.ReadAsync<OrderDocument>(
    documentId: "ord-123",
    partitionKey: "ord-123",
    options: null,
    storeName: "archive",
    cancellationToken: cancellationToken);
```

## Best Practices

- **Use `FindAsync` instead of `ReadAsync`** when document may not exist
- **Use `PagedQueryAsync` for large result sets** to avoid memory issues
- **Partition documents by access patterns** — prefer single-partition queries
- **Use `UpdateAsync` with retries** for safe concurrent updates
- **Use `ConditionalUpdateAsync` for optimistic concurrency** when version matters
- **Use transactions for multi-document updates** within the same partition
- **Leverage LINQ queries with `CreateQuery`** for type-safe querying
