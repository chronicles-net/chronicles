using Chronicles.Cosmos.Serialization;
using Microsoft.Azure.Cosmos;

namespace Chronicles.Cosmos.Internal;

public static class ItemResponseExtensions
{
    public static T GetItem<T>(
        this ItemResponse<T> response)
        where T : class
    {
        var result = response.Resource;
        if (result is ICosmosDocument { ETag: null } doc)
        {
            doc.ETag = response.ETag;
        }

        return result;
    }

    public static async Task<T> GetItemAsync<T>(
        this Task<ItemResponse<T>> responseTask)
        where T : class
        => GetItem(
            await responseTask.ConfigureAwait(false));

    public static T GetItemOrDefault<T>(
        this ItemResponse<object> response,
        IJsonCosmosSerializer serializer,
        T defaultValue)
        where T : ICosmosDocument
    {
        var result = defaultValue;

        if (response.Resource?.ToString() is { } json
            && serializer.FromString<T>(json) is { } obj)
        {
            result = obj;
        }

        if (result.ETag == null && response.ETag != null)
        {
            result.ETag = response.ETag;
        }

        return result;
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
