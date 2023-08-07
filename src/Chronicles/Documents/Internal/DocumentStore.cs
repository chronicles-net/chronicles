using Microsoft.Extensions.Options;

namespace Chronicles.Documents.Internal;

public class DocumentStore : IDocumentStore
{
    public DocumentStore(
        string name,
        IOptionsMonitor<DocumentOptions> documentOptions,
        IOptionsMonitor<InitializationOptions> initializationOptions)
    {
        Name = name;
        Options = documentOptions.Get(name);
        Initialization = initializationOptions.Get(name);
    }

    public string Name { get; }

    public DocumentOptions Options { get; }

    public InitializationOptions Initialization { get; }
}