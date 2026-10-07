using System.Net;
using Chronicles.Documents;
using Microsoft.Azure.Cosmos;

namespace Chronicles.EventStore.Internal;

/// <summary>
/// Document processor responsible for starting <see cref="IEventStreamProcessor"/> instances in parallel."/>
/// </summary>
/// <param name="exceptionHandler">Exception handler</param>
/// <param name="processors">Event stream processors</param>
internal class EventDocumentProcessor(
    IEventSubscriptionExceptionHandler exceptionHandler,
    IEnumerable<IEventStreamProcessor> processors)
    : IDocumentProcessor<StreamEvent>
{
    public async Task ErrorAsync(
      string leaseToken,
      Exception exception)
    {
        // Lease ownership moving to another instance (e.g. during scale-out or rollout)
        // is expected; the new owner continues from the last checkpoint.
        if (IsLeaseLost(exception))
        {
            return;
        }

        await exceptionHandler.HandleAsync(exception, null, CancellationToken.None);
    }

    public async Task ProcessAsync(
      IReadOnlyCollection<StreamEvent> changes,
      CancellationToken cancellationToken)
    {
        var processorTasks = processors
            .Select(async p => await ExecuteProcessorAsync(changes, p, cancellationToken))
            .ToArray();

        await Task.WhenAll(processorTasks);
    }

    private async Task ExecuteProcessorAsync(
        IReadOnlyCollection<StreamEvent> changes,
        IEventStreamProcessor processor,
        CancellationToken cancellationToken)
    {
        try
        {
            await processor.ProcessAsync(changes, cancellationToken);
        }
        catch (Exception e)
        {
            await exceptionHandler.HandleAsync(e, null, cancellationToken);
        }
    }

    private static bool IsLeaseLost(Exception exception)
    {
        if (exception is ChangeFeedProcessorUserException)
        {
            return false;
        }

        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is CosmosException { StatusCode: HttpStatusCode.PreconditionFailed }
                || current.GetType().Name == "LeaseLostException")
            {
                return true;
            }
        }

        return false;
    }
}