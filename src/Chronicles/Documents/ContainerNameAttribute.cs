namespace Chronicles.Documents;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class ContainerNameAttribute : Attribute
{
    public ContainerNameAttribute(
        string containerName)
    {
        ContainerName = containerName;
    }

    public string ContainerName { get; }

    public string? ClientName { get; set; }
}
