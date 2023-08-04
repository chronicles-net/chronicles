using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents.Internal;

public interface ICosmosSerializer
{
    CosmosPropertyNamingPolicy PropertyNamingPolicy { get; }

    [return: MaybeNull]
    T FromStream<T>(Stream stream);

    Stream ToStream<T>(T input);

    [return: MaybeNull]
    T FromString<T>(string json);
}
