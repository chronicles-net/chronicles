using System.Net.Sockets;
using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents.Internal;

public class DocumentStoreInitializer : IDocumentStoreInitializer
{
    private readonly ICosmosClientProvider provider;
    private readonly IEnumerable<IDocumentStore> stores;
    private readonly IContainerNameRegistry registry;

    public DocumentStoreInitializer(
        ICosmosClientProvider provider,
        IEnumerable<IDocumentStore> stores,
        IContainerNameRegistry registry)
    {
        this.provider = provider;
        this.stores = stores;
        this.registry = registry;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        foreach (var store in stores)
        {
            var database = await GetOrCreateDatabaseAsync(
                store,
                cancellationToken)
                .ConfigureAwait(false);

            foreach (var initializer in store.Options.Initialization.Containers)
            {
                var containerName = registry
                    .GetContainerName(initializer.DocumentType)
                    .ContainerName;

                await initializer.InitializeAsync(
                    database,
                    new()
                    {
                        Id = containerName,
                        PartitionKeyPath = "/id",
                    },
                    cancellationToken);
            }
        }
    }

    private async Task<Database> GetOrCreateDatabaseAsync(
        IDocumentStore store,
        CancellationToken cancellationToken)
    {
        try
        {
            if (store.Options.Initialization.Database == null)
            {
                return provider
                    .GetClient(store.Name)
                    .GetDatabase(store.Options.DatabaseName);
            }

            var response = await provider
                .GetClient(store.Name)
                .CreateDatabaseIfNotExistsAsync(
                    store.Options.DatabaseName,
                    store.Options.Initialization.Database,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return response.Database;
        }
        catch (Exception ex)
         when (IsCosmosEmulatorMissing(ex))
        {
            throw new InvalidOperationException(
                "Please start Cosmos DB Emulator");
        }
    }

    private bool IsCosmosEmulatorMissing(Exception ex)
        => provider.GetClient().Endpoint.IsLoopback
        && IsConnectionRefused(ex);

    private bool IsConnectionRefused(Exception ex) => ex switch
    {
        SocketException
        { SocketErrorCode: SocketError.ConnectionRefused }
            => true,

        AggregateException ae
        when ae.InnerExceptions.Any(IsCosmosEmulatorMissing)
            => true,

        Exception { InnerException: var inner }
        when inner != null
            => IsCosmosEmulatorMissing(inner),

        _ => false,
    };
}
