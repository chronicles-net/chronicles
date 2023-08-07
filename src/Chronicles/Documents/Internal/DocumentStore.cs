using Microsoft.Extensions.Options;

namespace Chronicles.Documents.Internal;

public class DocumentStore : IDocumentStore
{
    public DocumentStore(
        string name,
        IOptionsMonitor<DocumentOptions> optionsMonitor)
    {
        Name = name;
        Options = optionsMonitor.Get(name);
    }

    public string Name { get; }

    public DocumentOptions Options { get; }
}