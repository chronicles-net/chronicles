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
        this ItemResponse<T> response,
        T defaultValue)
        where T : IDocument
        => response.Resource ?? defaultValue;

    public static async Task<T> GetItemOrDefaultAsync<T>(
        this Task<ItemResponse<T>> responseTask,
        T defaultValue)
        where T : IDocument
        => (await responseTask.ConfigureAwait(false)).GetItemOrDefault(
            defaultValue);
}
