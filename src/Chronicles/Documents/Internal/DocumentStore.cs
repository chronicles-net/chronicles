using Microsoft.Extensions.Options;

namespace Chronicles.Documents.Internal;

internal class DocumentStore : IDocumentStore
{
    private readonly IOptionsMonitor<DocumentOptions> options;

    public DocumentStore(
        string name,
        IOptionsMonitor<DocumentOptions> options)
    {
        Name = name;
        this.options = options;
    }

    public string Name { get; }

    public DocumentOptions Options => options.Get(Name);
}
