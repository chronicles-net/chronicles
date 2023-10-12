using Microsoft.Azure.Cosmos;

namespace Chronicles.Documents.Internal;

/// <summary>
/// Represents a factory for creating a <see cref="ChangeFeedProcessor"/>
/// for a <see cref="IDocument"/>.
/// </summary>
public interface IChangeFeedFactory
{
    /// <summary>
    /// Create a <see cref="ChangeFeedProcessor"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="IDocument"/>.</typeparam>
    /// <param name="subscriptionName">A name that identifies the subscription.</param>
    /// <param name="onChanges">Delegate to receive changes.</param>
    /// <param name="onError">A delegate to receive notifications for change feed processor related errors.</param>
    /// <param name="storeName">(Optional) Name of the configured document store.</param>
    /// <returns>A <see cref="ChangeFeedProcessor"/>.</returns>
    ChangeFeedProcessor Create<T>(
        string subscriptionName,
        Container.ChangesHandler<T> onChanges,
        Container.ChangeFeedMonitorErrorDelegate? onError = null,
        string? storeName = null);
}
