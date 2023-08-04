namespace Chronicles.Documents;

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class ContainerNameAttribute : Attribute
{
    public ContainerNameAttribute(
        string containerName)
    {
        ContainerName = containerName;
    }

    public string ContainerName { get; }

    public string? StoreName { get; set; }
}
