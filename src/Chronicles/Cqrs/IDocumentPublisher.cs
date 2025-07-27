using Chronicles.Documents;

namespace Chronicles.Cqrs;

/// <summary>
/// Defines a contract for publishing documents of type <typeparamref name="TDocument"/>.
/// </summary>
/// <typeparam name="TDocument">
/// The type of document to publish. Must implement <see cref="IDocument"/>.
/// </typeparam>
public interface IDocumentPublisher<TDocument>
    where TDocument : class, IDocument
{
    /// <summary>
    /// Publishes the specified document asynchronously.
    /// </summary>
    /// <param name="document">The document to publish.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous publish operation.</returns>
    Task PublishAsync(
        TDocument document,
        CancellationToken cancellationToken);
}
