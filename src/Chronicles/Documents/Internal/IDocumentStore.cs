namespace Chronicles.Documents.Internal;

public interface IDocumentStore
{
    string Name { get; }

    DocumentOptions Options { get; }

    InitializationOptions Initialization { get; }
}
