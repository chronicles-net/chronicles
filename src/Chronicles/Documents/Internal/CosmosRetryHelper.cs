using System.Net;
using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents.Internal;

/// <summary>
/// Retries Cosmos DB database/container creation when it fails with the transient
/// "not yet available for read" error (404, substatus 1013). This happens when two
/// or more processes race to create the same database or container concurrently
/// (most commonly in local Aspire setups where multiple services/processors start at
/// the same time against the Cosmos DB emulator): the loser of the race sees its own
/// create call answered with 409 Conflict, and the Cosmos SDK's built-in read-after-409
/// fallback can still observe the container as not-yet-readable for longer than its own
/// internal retry window, causing it to bubble up an exception even though the resource
/// is actually being created successfully by the other process.
/// </summary>
internal static class CosmosRetryHelper
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(10);

    public static async Task ExecuteWithRetryAsync(
        Func<Task> operation,
        CancellationToken cancellationToken,
        int maxAttempts = 6,
        Func<TimeSpan, CancellationToken, Task>? delayAsync = null)
    {
        await ExecuteWithRetryAsync(
            async () =>
            {
                await operation().ConfigureAwait(false);
                return true;
            },
            cancellationToken,
            maxAttempts,
            delayAsync);
    }

    public static async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken,
        int maxAttempts = 6,
        Func<TimeSpan, CancellationToken, Task>? delayAsync = null)
    {
        delayAsync ??= Task.Delay;
        var delay = InitialDelay;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await operation().ConfigureAwait(false);
            }
            catch (CosmosException ex)
                when (attempt < maxAttempts && IsCreationRace(ex))
            {
                await delayAsync(delay, cancellationToken)
                    .ConfigureAwait(false);

                delay = delay + delay > MaxDelay
                    ? MaxDelay
                    : delay + delay;
            }
        }
    }

    private static bool IsCreationRace(CosmosException ex)
        => ex is { StatusCode: HttpStatusCode.NotFound, SubStatusCode: 1013 };
}
