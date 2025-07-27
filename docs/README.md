# Chronicles Library Developer Documentation

[![Branch Coverage](../.github/coveragereport/badge_branchcoverage.svg?raw=true)](https://github.com/chronicles-net/chronicles/actions/workflows/ci.yml)
[![Line Coverage](../.github/coveragereport/badge_linecoverage.svg?raw=true)](https://github.com/chronicles-net/chronicles/actions/workflows/ci.yml)
[![Method Coverage](../.github/coveragereport/badge_methodcoverage.svg?raw=true)](https://github.com/chronicles-net/chronicles/actions/workflows/ci.yml)

## 🚀 Introduction

Chronicles is a powerful .NET library that provides a comprehensive foundation for building event-sourced applications using the CQRS (Command Query Responsibility Segregation) pattern. Built on top of Azure Cosmos DB, Chronicles offers a robust, scalable, and developer-friendly approach to implementing event sourcing architectures.

### 🎯 Design Philosophy

Chronicles is designed with the following principles in mind:

- **Simplicity First**: Clean, intuitive APIs that make complex event sourcing concepts accessible
- **Performance**: Optimized for high-throughput scenarios with minimal overhead
- **Testability**: Comprehensive testing utilities and patterns built-in
- **Flexibility**: Supports various architectural patterns while maintaining consistency
- **Developer Experience**: Rich documentation, clear error messages, and helpful tooling

### 🏗️ Core Architecture

Chronicles is organized into four primary layers, each serving a specific purpose:

```
┌─────────────────┐
│   CQRS Layer    │  ← Commands, Queries, Handlers, State Management
├─────────────────┤
│ EventStore Layer│  ← Event Streaming, Persistence, Versioning
├─────────────────┤
│ Documents Layer │  ← Core Cosmos DB Abstractions
├─────────────────┤
│  Testing Layer  │  ← Fakes, Mocks, Testing Utilities
└─────────────────┘
```

### 📖 Documentation Structure

This documentation is organized to provide both educational content and practical guidance:

#### 🎓 Learning Resources
- **[Event Sourcing Primer](./event-sourcing-primer.md)** - Introduction to event sourcing concepts
- **[Getting Started Guide](./getting-started.md)** - Quick start tutorial

#### 🔧 Layer Documentation
- **[Documents Layer](./documents-layer.md)** - Core document concepts and data access patterns
- **[EventStore Layer](./eventstore-layer.md)** - Event streaming, storage, and retrieval
- **[CQRS Layer](./cqrs-layer.md)** - Command and query patterns, handlers, projections
- **[Testing Layer](./testing-layer.md)** - Testing utilities and best practices

#### 📚 Advanced Topics
- **[Best Practices](./best-practices.md)** - Recommended patterns and conventions
- **[Performance Guide](./performance-guide.md)** - Optimization techniques and considerations
- **[Troubleshooting](./troubleshooting.md)** - Common issues and solutions

### 🎯 Common Use Cases

Chronicles excels in scenarios requiring:

- **Audit Trails**: Complete history of all changes to business entities
- **Temporal Queries**: Understanding system state at any point in time
- **Event-Driven Architectures**: Decoupled, scalable microservices
- **Complex Business Logic**: Rich domain models with event-driven state changes
- **High Availability**: Distributed systems with eventual consistency
- **Compliance**: Immutable event logs for regulatory requirements

### 🚦 Quick Start

```csharp
// 1. Configure Chronicles in your startup
services.AddChronicles(options =>
{
    options.AddDocumentStore("cosmos-connection-string");
});

// 2. Define your events
public record OrderCreated(string OrderId, decimal Amount);

// 3. Create a command handler
public class CreateOrderHandler : ICommandHandler<CreateOrder, OrderAggregate>
{
    public ValueTask ExecuteAsync(ICommandContext<CreateOrder> context, OrderAggregate state, CancellationToken cancellationToken)
    {
        var orderCreated = new OrderCreated(context.Command.OrderId, context.Command.Amount);
        context.AddEvent(orderCreated);
        return ValueTask.CompletedTask;
    }
    
    // State projection logic...
}

// 4. Execute commands
await commandProcessor.ExecuteAsync(new CreateOrder("order-123", 99.99m));
```

### 🔗 Navigation

Choose your learning path:

- **New to Event Sourcing?** → Start with [Event Sourcing Primer](./event-sourcing-primer.md)
- **Ready to Code?** → Jump to [Getting Started Guide](./getting-started.md)  
- **Need Specific Info?** → Browse the layer documentation above
- **Looking for Examples?** → Check out the [sample applications](../sample/)

---

> 💡 **Tip**: Each section includes comprehensive code examples and practical scenarios to help you understand and implement Chronicles effectively in your applications.