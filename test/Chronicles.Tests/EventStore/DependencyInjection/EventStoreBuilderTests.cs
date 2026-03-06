using Chronicles.Documents.DependencyInjection;
using Chronicles.EventStore.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Chronicles.Tests.EventStore.DependencyInjection;

public class EventStoreBuilderTests
{
    internal sealed record EventA();

    internal sealed record EventB();

    [Fact]
    internal void Build_Should_Throw_When_Alias_Conflicts_With_Primary_Name()
    {
        var sut = CreateBuilder();
        sut.AddEvent<EventA>("event-a");
        sut.AddEvent<EventB>("event-b", "event-a");

        FluentActions
            .Invoking(() => sut.Build())
            .Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*'event-a'*already registered*");
    }

    [Fact]
    internal void Build_Should_Throw_When_Duplicate_Alias_Is_Registered()
    {
        var sut = CreateBuilder();
        sut.AddEvent<EventA>("event-a", "legacy-name");
        sut.AddEvent<EventB>("event-b", "legacy-name");

        FluentActions
            .Invoking(() => sut.Build())
            .Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*'legacy-name'*already registered*");
    }

    [Fact]
    internal void Build_Should_Succeed_When_No_Conflicts_Exist()
    {
        var sut = CreateBuilder();
        sut.AddEvent<EventA>("event-a", "old-event-a");
        sut.AddEvent<EventB>("event-b", "old-event-b");

        FluentActions
            .Invoking(() => sut.Build())
            .Should()
            .NotThrow();
    }

    private static EventStoreBuilder CreateBuilder()
        => new(new DocumentStoreBuilder("test-store", new ServiceCollection()));
}
