using System.Collections.Concurrent;

namespace Chronicles.Documents.Internal;

public class CosmosSerializerProvider : ICosmosSerializerProvider
{
    private readonly ConcurrentDictionary<Type, ICosmosSerializer> serializers = new();
    private readonly IContainerNameRegistry registry;
    private readonly ICosmosClientProvider clientProvider;

    public CosmosSerializerProvider(
        IContainerNameRegistry registry,
        ICosmosClientProvider clientProvider)
    {
        this.registry = registry;
        this.clientProvider = clientProvider;
    }

    public ICosmosSerializer GetSerializer<T>()
        => GetSerializer(typeof(T));

    public ICosmosSerializer GetSerializer(
        Type documentType)
    {
        if (serializers.TryGetValue(documentType, out var serializer))
        {
            return serializer;
        }

        var name = registry.GetContainerName(documentType);
        return serializers
            .GetOrAdd(
                documentType,
                t => GetSerializer(name.StoreName));
    }

    public ICosmosSerializer GetSerializer(
        string? storeName = default)
        => clientProvider.GetSerializer(storeName);
}
