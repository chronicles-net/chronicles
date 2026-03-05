using NetArchTest.Rules;
using Xunit;

namespace Chronicles.Tests.Architecture;

/// <summary>
/// Enforces the architectural layering directive issued by Lars Skovslund (2026-03-05):
///   1. Documents — lowest level, no upward references
///   2. EventStore — uses only PUBLIC types from Documents
///   3. CQRS — uses only PUBLIC types from EventStore and Documents
///
/// Explicit exceptions are documented inline.
/// </summary>
public class LayerBoundaryTests
{
    private static readonly System.Reflection.Assembly Assembly =
        typeof(Chronicles.Documents.IDocument).Assembly;

    [Fact]
    public void Documents_should_not_reference_EventStore()
    {
        var result = Types.InAssembly(Assembly)
            .That()
            .ResideInNamespace("Chronicles.Documents")
            .ShouldNot()
            .HaveDependencyOn("Chronicles.EventStore")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Documents layer must not reference EventStore. Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Documents_should_not_reference_Cqrs()
    {
        var result = Types.InAssembly(Assembly)
            .That()
            .ResideInNamespace("Chronicles.Documents")
            .ShouldNot()
            .HaveDependencyOn("Chronicles.Cqrs")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Documents layer must not reference Cqrs. Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void EventStore_should_not_reference_Cqrs()
    {
        var result = Types.InAssembly(Assembly)
            .That()
            .ResideInNamespace("Chronicles.EventStore")
            .ShouldNot()
            .HaveDependencyOn("Chronicles.Cqrs")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"EventStore layer must not reference Cqrs. Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void EventStore_should_not_reference_Documents_Internal()
    {
        // Explicit exception: EventStoreBuilder in DependencyInjection is permitted
        // to reference Documents.Internal for DI wiring of change-feed subscriptions.
        // See explicit exception comment in EventStoreBuilder.cs (approved: Lars Skovslund, 2026-03-05).
        //
        // Therefore, this test excludes the DependencyInjection namespace from the check.
        var result = Types.InAssembly(Assembly)
            .That()
            .ResideInNamespace("Chronicles.EventStore")
            .And()
            .DoNotResideInNamespace("Chronicles.EventStore.DependencyInjection")
            .ShouldNot()
            .HaveDependencyOn("Chronicles.Documents.Internal")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"EventStore layer (excluding DI wiring) must not reference Documents.Internal. " +
            $"Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Cqrs_should_not_reference_EventStore_Internal()
    {
        var result = Types.InAssembly(Assembly)
            .That()
            .ResideInNamespace("Chronicles.Cqrs")
            .ShouldNot()
            .HaveDependencyOn("Chronicles.EventStore.Internal")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Cqrs layer must not reference EventStore.Internal. " +
            $"Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Cqrs_should_not_reference_Documents_Internal()
    {
        var result = Types.InAssembly(Assembly)
            .That()
            .ResideInNamespace("Chronicles.Cqrs")
            .ShouldNot()
            .HaveDependencyOn("Chronicles.Documents.Internal")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Cqrs layer must not reference Documents.Internal. " +
            $"Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}
