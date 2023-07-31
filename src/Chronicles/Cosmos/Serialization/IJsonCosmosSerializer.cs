using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Chronicles.Cosmos.Serialization;

public interface IJsonCosmosSerializer
{
    [return: MaybeNull]
    T FromStream<T>(Stream stream);

    Stream ToStream<T>(T input);

    [return: MaybeNull]
    T FromString<T>(string json);

    [return: MaybeNull]
    T FromJson<T>(JsonElement json);
}
