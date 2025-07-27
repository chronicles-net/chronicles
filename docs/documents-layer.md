# Documents Layer

The Documents layer provides the foundational abstractions for working with Azure Cosmos DB documents in Chronicles. It offers a clean, strongly-typed interface for document operations while handling the complexities of Cosmos DB interactions.

## 🎯 Core Concepts

### Documents as Domain Objects

In Chronicles, documents represent your domain entities that are persisted in Cosmos DB. All documents must implement the `IDocument` interface:

```csharp
public interface IDocument
{
    string GetDocumentId();     // Unique identifier within the partition
    string GetPartitionKey();   // Cosmos DB partition key for distribution
}
```

### Document Base Class

Chronicles provides a convenient base class for your documents:

```csharp
public abstract class Document : IDocument
{
    protected abstract string GetDocumentId();
    protected abstract string GetPartitionKey();
}
```

## 📝 Creating Documents

### Simple Document Example

```csharp
public class CustomerDocument : Document
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string TenantId { get; set; } = string.Empty;

    protected override string GetDocumentId() => Id;
    protected override string GetPartitionKey() => TenantId;
}
```

### Complex Document with Nested Data

```csharp
public class OrderDocument : Document
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = new();
    public OrderStatus Status { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
    
    protected override string GetDocumentId() => OrderId;
    protected override string GetPartitionKey() => CustomerId;
}

public class OrderItem
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public enum OrderStatus
{
    Created,
    Paid,
    Shipped,
    Delivered,
    Cancelled
}
```

## 📖 Document Reading

The `IDocumentReader<T>` interface provides various ways to read documents from Cosmos DB:

### Reading by ID

```csharp
public class CustomerService
{
    private readonly IDocumentReader<CustomerDocument> _reader;

    public CustomerService(IDocumentReader<CustomerDocument> reader)
    {
        _reader = reader;
    }

    public async Task<CustomerDocument> GetCustomerAsync(string customerId, string tenantId)
    {
        return await _reader.ReadAsync<CustomerDocument>(
            documentId: customerId,
            partitionKey: tenantId
        );
    }

    public async Task<CustomerDocument?> TryGetCustomerAsync(string customerId, string tenantId)
    {
        try
        {
            return await _reader.ReadAsync<CustomerDocument>(customerId, tenantId, null);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
```

### Querying Documents

```csharp
public class CustomerQueryService
{
    private readonly IDocumentReader<CustomerDocument> _reader;

    public CustomerQueryService(IDocumentReader<CustomerDocument> reader)
    {
        _reader = reader;
    }

    // Query using LINQ expressions
    public async Task<List<CustomerDocument>> GetActiveCustomersAsync(string tenantId)
    {
        var query = _reader.CreateQuery<CustomerDocument>(
            q => q.Where(c => c.TenantId == tenantId && c.Status == CustomerStatus.Active)
        );

        var results = new List<CustomerDocument>();
        await foreach (var customer in _reader.QueryAsync<CustomerDocument>(query, tenantId))
        {
            results.Add(customer);
        }
        return results;
    }

    // Query with custom SQL
    public async Task<List<CustomerSummary>> GetCustomerSummariesAsync(string tenantId)
    {
        var query = new QueryDefinition(
            "SELECT c.Id, c.Name, c.Email, c.CreatedAt FROM c WHERE c.TenantId = @tenantId"
        ).WithParameter("@tenantId", tenantId);

        var results = new List<CustomerSummary>();
        await foreach (var summary in _reader.QueryAsync<CustomerSummary>(query, tenantId))
        {
            results.Add(summary);
        }
        return results;
    }
}

public class CustomerSummary
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
```

### Paginated Queries

```csharp
public async Task<PagedResult<CustomerDocument>> GetCustomersPagedAsync(
    string tenantId, 
    int pageSize, 
    string? continuationToken = null)
{
    var query = new QueryDefinition(
        "SELECT * FROM c WHERE c.TenantId = @tenantId ORDER BY c.CreatedAt DESC"
    ).WithParameter("@tenantId", tenantId);

    return await _reader.PagedQueryAsync<CustomerDocument>(
        query: query,
        partitionKey: tenantId,
        maxItemCount: pageSize,
        continuationToken: continuationToken
    );
}

// Usage
var firstPage = await GetCustomersPagedAsync("tenant-1", pageSize: 20);
var secondPage = await GetCustomersPagedAsync("tenant-1", pageSize: 20, firstPage.ContinuationToken);
```

## ✏️ Document Writing

The `IDocumentWriter<T>` interface provides comprehensive document manipulation capabilities:

### Creating Documents

```csharp
public class CustomerWriteService
{
    private readonly IDocumentWriter<CustomerDocument> _writer;

    public CustomerWriteService(IDocumentWriter<CustomerDocument> writer)
    {
        _writer = writer;
    }

    public async Task<CustomerDocument> CreateCustomerAsync(
        string name, 
        string email, 
        string tenantId)
    {
        var customer = new CustomerDocument
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Email = email,
            TenantId = tenantId,
            CreatedAt = DateTime.UtcNow,
            Status = CustomerStatus.Active
        };

        return await _writer.CreateAsync(customer, new ItemRequestOptions());
    }
}
```

### Updating Documents

```csharp
public async Task<CustomerDocument> UpdateCustomerEmailAsync(
    string customerId, 
    string tenantId, 
    string newEmail)
{
    return await _writer.UpdateAsync(
        documentId: customerId,
        partitionKey: tenantId,
        updateDocument: async customer =>
        {
            customer.Email = newEmail;
            customer.UpdatedAt = DateTime.UtcNow;
            return customer;
        },
        retries: 3 // Automatic retry on conflicts
    );
}

public async Task<CustomerDocument> UpdateOrCreateCustomerAsync(
    string customerId,
    string tenantId,
    string name,
    string email)
{
    return await _writer.UpdateOrCreateAsync(
        getDefaultDocument: () => new CustomerDocument
        {
            Id = customerId,
            TenantId = tenantId,
            CreatedAt = DateTime.UtcNow,
            Status = CustomerStatus.Active
        },
        updateDocument: async customer =>
        {
            customer.Name = name;
            customer.Email = email;
            customer.UpdatedAt = DateTime.UtcNow;
            return customer;
        }
    );
}
```

### Upserting Documents

```csharp
public async Task<CustomerDocument> UpsertCustomerAsync(CustomerDocument customer)
{
    // WriteAsync performs upsert behavior (create or replace)
    return await _writer.WriteAsync(customer, new ItemRequestOptions
    {
        EnableContentResponseOnWrite = true // Return the updated document
    });
}
```

### Deleting Documents

```csharp
public async Task DeleteCustomerAsync(string customerId, string tenantId)
{
    await _writer.DeleteAsync(
        documentId: customerId,
        partitionKey: tenantId,
        options: new ItemRequestOptions()
    );
}

public async Task DeleteAllCustomersInTenantAsync(string tenantId)
{
    // Delete all documents in a partition
    await _writer.DeletePartitionAsync(
        partitionKey: tenantId,
        options: new ItemRequestOptions()
    );
}
```

## 🔄 Transactions

Chronicles supports transactional operations within a single partition:

```csharp
public async Task TransferOrderAsync(
    string orderId, 
    string fromCustomerId, 
    string toCustomerId)
{
    // Both customers must be in the same partition for this to work
    if (GetTenantId(fromCustomerId) != GetTenantId(toCustomerId))
        throw new InvalidOperationException("Cross-partition transactions not supported");

    var tenantId = GetTenantId(fromCustomerId);
    
    using var transaction = _orderWriter.CreateTransaction(tenantId);
    
    // Update order to new customer
    await transaction.ReplaceAsync(new OrderDocument
    {
        OrderId = orderId,
        CustomerId = toCustomerId,
        // ... other properties
    });
    
    // Update customer order counts
    await transaction.UpdateAsync(
        documentId: fromCustomerId,
        updateDocument: async customer =>
        {
            customer.OrderCount--;
            return customer;
        }
    );
    
    await transaction.UpdateAsync(
        documentId: toCustomerId,
        updateDocument: async customer =>
        {
            customer.OrderCount++;
            return customer;
        }
    );
    
    // Commit all operations atomically
    await transaction.ExecuteAsync();
}
```

## 🔧 Configuration

### Setting up Document Stores

```csharp
// In your Startup.cs or Program.cs
services.AddChronicles(options =>
{
    // Primary document store
    options.AddDocumentStore("primary", cosmosConnectionString);
    
    // Secondary read-only store
    options.AddDocumentStore("readonly", readOnlyConnectionString);
});

// Register document readers/writers
services.AddTransient<IDocumentReader<CustomerDocument>, DocumentReader<CustomerDocument>>();
services.AddTransient<IDocumentWriter<CustomerDocument>, DocumentWriter<CustomerDocument>>();
```

### Container Configuration

Use the `ContainerName` attribute to specify custom container names:

```csharp
[ContainerName("customers")]
public class CustomerDocument : Document
{
    // ...
}

[ContainerName("orders")]  
public class OrderDocument : Document
{
    // ...
}
```

### Multi-Store Usage

```csharp
public class CustomerService
{
    private readonly IDocumentReader<CustomerDocument> _reader;

    public async Task<CustomerDocument> GetCustomerFromSecondaryAsync(string id, string tenantId)
    {
        // Read from specific named store
        return await _reader.ReadAsync<CustomerDocument>(
            documentId: id,
            partitionKey: tenantId,
            options: null,
            storeName: "readonly" // Use secondary store
        );
    }
}
```

## 📊 Performance Considerations

### Optimizing Queries

```csharp
// ✅ Good: Query within partition
var customers = _reader.QueryAsync<CustomerDocument>(
    query, 
    partitionKey: tenantId // Partition-scoped query
);

// ❌ Avoid: Cross-partition queries when possible
var customers = _reader.QueryAsync<CustomerDocument>(
    query, 
    partitionKey: null // Cross-partition query
);
```

### Request Options

```csharp
// Optimize for write performance
var writeOptions = new ItemRequestOptions
{
    EnableContentResponseOnWrite = false, // Don't return document content
    ConsistencyLevel = ConsistencyLevel.Session
};

// Optimize for read performance  
var readOptions = new QueryRequestOptions
{
    ConsistencyLevel = ConsistencyLevel.Eventual,
    MaxItemCount = 100 // Batch size
};
```

## 🧪 Testing

Chronicles provides fake implementations for testing:

```csharp
[Test]
public async Task Should_Create_Customer_Successfully()
{
    // Arrange
    var fakeWriter = new FakeDocumentWriter<CustomerDocument>();
    var service = new CustomerService(fakeWriter);
    
    // Act
    var customer = await service.CreateCustomerAsync("John Doe", "john@example.com", "tenant-1");
    
    // Assert
    Assert.That(customer.Name, Is.EqualTo("John Doe"));
    Assert.That(fakeWriter.CreatedDocuments, Has.Count.EqualTo(1));
}
```

## 🚀 Next Steps

Now that you understand the Documents layer, explore:

- **[EventStore Layer](./eventstore-layer.md)** - Learn about event persistence and streaming
- **[CQRS Layer](./cqrs-layer.md)** - Discover command and query patterns
- **[Testing Layer](./testing-layer.md)** - Master testing techniques

---

> 💡 **Best Practice**: Design your partition keys carefully to ensure even distribution and enable efficient queries. Most Chronicles applications partition by tenant ID or aggregate root ID.