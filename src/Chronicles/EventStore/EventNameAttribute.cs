namespace Chronicles.EventStore;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class EventNameAttribute : Attribute
{
    public EventNameAttribute(string name)
    {
        Name = name;
    }

    public EventName Name { get; }
}