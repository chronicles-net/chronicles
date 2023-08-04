using System.Diagnostics.CodeAnalysis;

namespace Chronicles.Documents.Internal;

public class CosmosSerializerAdapter : Microsoft.Azure.Cosmos.CosmosSerializer
{
    public CosmosSerializerAdapter(ICosmosSerializer serializer)
    {
        Serializer = serializer;
    }

    public ICosmosSerializer Serializer { get; }

    [return: MaybeNull]
    public override T FromStream<T>(Stream stream)
        => Serializer.FromStream<T>(stream);

    public override Stream ToStream<T>(T input)
        => Serializer.ToStream(input);
}