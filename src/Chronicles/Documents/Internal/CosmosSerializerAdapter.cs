using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Chronicles.Documents.Internal;

public class CosmosSerializerAdapter : Microsoft.Azure.Cosmos.CosmosLinqSerializer
{
    public CosmosSerializerAdapter(ICosmosSerializer serializer)
    {
        Serializer = serializer;
    }

    public ICosmosSerializer Serializer { get; }

    [return: MaybeNull]
    public override T FromStream<T>(Stream stream)
        => Serializer.FromStream<T>(stream);

    public override string SerializeMemberName(MemberInfo memberInfo)
        => Serializer.SerializeMemberName(memberInfo);

    public override Stream ToStream<T>(T input)
        => Serializer.ToStream(input);
}