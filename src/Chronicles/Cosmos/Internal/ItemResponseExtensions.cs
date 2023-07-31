using Chronicles.Cosmos.Serialization;
using Microsoft.Azure.Cosmos;

namespace Chronicles.Cosmos.Internal;

public static class ItemResponseExtensions
{
    public static async Task<T> GetItemAsync<T>(
        this Task<ItemResponse<T>> responseTask)
        where T : class
    {
        var response = await responseTask.ConfigureAwait(false);
        return response.Resource;
    }

    public static T GetItemOrDefault<T>(
        this ItemResponse<object> response,
        IJsonCosmosSerializer serializer,
        T defaultValue)
        where T : ICosmosDocument
    {
        if (response.Resource?.ToString() is { } json
            && serializer.FromString<T>(json) is { } obj)
        {
            return obj;
        }

        return defaultValue;
    }

    public static async Task<T> GetItemOrDefaultAsync<T>(
        this Task<ItemResponse<object>> responseTask,
        IJsonCosmosSerializer serializer,
        T defaultValue)
        where T : ICosmosDocument
        => GetItemOrDefault(
            await responseTask.ConfigureAwait(false),
            serializer,
            defaultValue);
}
