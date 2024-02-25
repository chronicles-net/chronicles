namespace Chronicles.EventStore.Internal.Commands;

public static class CommandExtensions
{
    public static StreamReadOptions? GetStreamReadOptions(
        this CommandOptions? options)
        => new()
        {
            RequiredVersion = options?.RequiredVersion,
            RequiredState = options?.RequiredState,
        };

    public static StreamWriteOptions? GetStreamWriteOptions(
        this CommandOptions? options)
        => options switch
        {
            { } o => new StreamWriteOptions() { CorrelationId = o.CorrelationId, CausationId = o.CommandId },
            _ => null,
        };

    public static int NextRerunCount(
        this CommandOptions? options)
        => options switch
        {
            { } o when o.Behavior == OnConflict.RerunCommand => o.BehaviorCount--,
            _ => 0,
        };
}