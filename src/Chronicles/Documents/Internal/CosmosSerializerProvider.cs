using System.Collections.Concurrent;
using System.Reflection;

namespace Chronicles.Documents.Internal;

public class CosmosSerializerProvider : ICosmosSerializerProvider
{
    private readonly ConcurrentDictionary<Type, ICosmosSerializer> serializers = new();
    private readonly ICosmosClientProvider clientProvider;

    public CosmosSerializerProvider(
        ICosmosClientProvider clientProvider)
    {
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

        if (documentType.GetCustomAttribute<ContainerNameAttribute>(inherit: true) is not { } a)
        {
            throw new ArgumentException(
                $"Type {documentType.Name} is not supported. " +
                $"Missing {nameof(ContainerNameAttribute)}.",
                nameof(documentType));
        }

        return serializers
            .GetOrAdd(
                documentType,
                t => GetSerializer(a.ClientName));
    }

    public ICosmosSerializer GetSerializer(
        string? clientName = default)
        => clientProvider.GetSerializer(clientName);
}
