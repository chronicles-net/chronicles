using System.Net;
using Chronicles.Documents.Internal;
using Microsoft.Azure.Cosmos;

namespace Chronicles.Tests.Documents.Internal;

public class CosmosRetryHelperTests
{
    private static CosmosException CreationRaceException()
        => new(
            "Collection is not yet available for read",
            HttpStatusCode.NotFound,
            subStatusCode: 1013,
            activityId: Guid.NewGuid().ToString(),
            requestCharge: 0);

    [Fact]
    public async Task ExecuteWithRetryAsync_Returns_Result_On_First_Success()
    {
        var result = await CosmosRetryHelper.ExecuteWithRetryAsync(
            () => Task.FromResult(42),
            CancellationToken.None);

        result.Should().Be(42);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_Retries_On_Creation_Race_Then_Succeeds()
    {
        var attempts = 0;
        var delays = new List<TimeSpan>();

        var result = await CosmosRetryHelper.ExecuteWithRetryAsync(
            () =>
            {
                attempts++;
                if (attempts < 3)
                {
                    throw CreationRaceException();
                }

                return Task.FromResult(attempts);
            },
            CancellationToken.None,
            delayAsync: (delay, _) =>
            {
                delays.Add(delay);
                return Task.CompletedTask;
            });

        result.Should().Be(3);
        attempts.Should().Be(3);
        delays.Should().Equal(
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_Stops_After_MaxAttempts()
    {
        var attempts = 0;
        var delays = new List<TimeSpan>();

        Func<Task> act = () => CosmosRetryHelper.ExecuteWithRetryAsync(
            () =>
            {
                attempts++;
                throw CreationRaceException();
            },
            CancellationToken.None,
            maxAttempts: 3,
            delayAsync: (delay, _) =>
            {
                delays.Add(delay);
                return Task.CompletedTask;
            });

        await act.Should().ThrowAsync<CosmosException>();
        attempts.Should().Be(3);
        delays.Should().Equal(
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_Does_Not_Retry_Other_Cosmos_Exceptions()
    {
        var attempts = 0;

        Func<Task> act = () => CosmosRetryHelper.ExecuteWithRetryAsync(
            () =>
            {
                attempts++;
                throw new CosmosException(
                    "Conflict",
                    HttpStatusCode.Conflict,
                    subStatusCode: 0,
                    activityId: Guid.NewGuid().ToString(),
                    requestCharge: 0);
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<CosmosException>();
        attempts.Should().Be(1);
    }
}
