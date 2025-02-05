using Chronicles.Documents;

namespace Chronicles.Cqrs;

public interface IDocumentProjection<TDocument>
    : IStateProjection<TDocument>
    where TDocument : class, IDocument
{
    ValueTask<DocumentCommitAction> OnCommitAsync(
        TDocument document,
        CancellationToken cancellationToken)
        => ValueTask.FromResult(
            DocumentCommitAction.Update);
}

public enum DocumentCommitAction
{
    Update,
    Delete,
    None,
}
