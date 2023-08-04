using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents.Internal;

public static class ItemResponseExtensions
{
    public static async Task<T> GetItemAsync<T>(
        this Task<ItemResponse<T>> responseTask)
    {
        var response = await responseTask.ConfigureAwait(false);
        return response.Resource;
    }

    public static T GetItemOrDefault<T>(
        this ItemResponse<object> response,
        ICosmosSerializer serializer,
        T defaultValue)
        where T : IDocument
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
        ICosmosSerializer serializer,
        T defaultValue)
        where T : IDocument
        => (await responseTask.ConfigureAwait(false)).GetItemOrDefault(
            serializer,
            defaultValue);
}
