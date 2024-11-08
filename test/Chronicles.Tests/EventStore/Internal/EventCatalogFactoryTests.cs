using Chronicles.EventStore;
using Chronicles.EventStore.Internal;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Chronicles.Tests.EventStore.Internal;

public class EventCatalogFactoryTests
{
    [Theory, AutoNSubstituteData]
    internal void Get_Returns_EventCatalog(
        [Frozen(Matching.ImplementedInterfaces)] IKeyedServiceProvider serviceProvider,
        IEventCatalog eventCatalog,
        EventCatalogFactory sut)
    {
        serviceProvider
            .GetRequiredKeyedService(typeof(IEventCatalog), "StoreName")
            .Returns(eventCatalog);

        sut
            .Get("StoreName")
            .Should()
            .Be(eventCatalog);
    }

    [Theory, AutoNSubstituteData]
    internal void Get_Should_Use_Default_StoreName_When_Null(
        [Frozen(Matching.ImplementedInterfaces)] IKeyedServiceProvider serviceProvider,
        EventCatalogFactory sut)
    {
        sut.Get(null);

        serviceProvider
            .Received()
            .GetRequiredKeyedService(typeof(IEventCatalog), string.Empty);
    }
}