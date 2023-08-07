using Chronicles.Documents;

namespace Chronicles.Documents.Internal;

public interface IDocumentStore
{
    public string Name { get; }

    public DocumentOptions Options { get; }
}
