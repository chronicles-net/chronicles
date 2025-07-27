using Chronicles.Documents;

namespace Chronicles.Cqrs;

/// <summary>
/// Defines a projection for documents that supports state projection and commit actions.
/// </summary>
/// <remarks>Implement <see cref="OnCommitAsync(TDocument, CancellationToken)"/> to control what <see cref="DocumentCommitAction"/> to perform when projection is completed.</remarks>
/// <typeparam name="TDocument">The type of the document being projected.</typeparam>
public interface IDocumentProjection<TDocument>
    : IStateProjection<TDocument>
    where TDocument : class, IDocument
{
    /// <summary>
    /// Invoked when a document is committed, allowing custom commit actions.
    /// </summary>
    /// <param name="document">The document being committed.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="DocumentCommitAction"/> indicating the commit action to perform.</returns>
    ValueTask<DocumentCommitAction> OnCommitAsync(
        TDocument document,
        CancellationToken cancellationToken)
        => ValueTask.FromResult(
            DocumentCommitAction.Update);
}
