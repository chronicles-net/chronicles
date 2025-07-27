using Chronicles.Documents;
using Chronicles.Documents.DependencyInjection;
using Chronicles.Documents.Internal;
using Chronicles.Testing;
using Microsoft.Extensions.Options;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class ServiceCollectionForTestingExtensions
{
    public static ChroniclesBuilder AddFakeChronicles(
        this IServiceCollection services,
        Action<DocumentStoreBuilder>? builder = null)
    {
        builder?.Invoke(new(DocumentOptions.DefaultStoreName, services));
        services.AddSingleton<IDocumentStore>(s => new DocumentStore(
            DocumentOptions.DefaultStoreName,
            s.GetRequiredService<IOptionsMonitor<DocumentOptions>>()));

        services
            .AddSingleton(typeof(IDocumentReader<>), typeof(FakeDocumentReader<>))
            .AddSingleton(typeof(IDocumentWriter<>), typeof(FakeDocumentWriter<>))
            .AddSingleton<IFakeDocumentStoreProvider, FakeDocumentStoreProvider>()
            .AddSingleton<IContainerNameRegistry, ContainerNameRegistry>()
            .AddSingleton<IChangeFeedFactory, FakeChangeFeedFactory>()
            .AddSingleton<ISubscriptionService, SubscriptionService>()
            .AddSingleton<IDocumentStoreInitializer, FakeDocumentStoreInitializer>()
            .AddHostedService<DocumentStoreService>();

        return new ChroniclesBuilder(services);
    }
}
