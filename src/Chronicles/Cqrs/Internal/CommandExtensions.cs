using Chronicles.EventStore;

namespace Chronicles.Cqrs.Internal;

internal static class CommandExtensions
{
    public static StreamReadOptions GetStreamReadOptions(
        this CommandRequestOptions options,
        StreamMetadata metadata,
        CommandOptions commandOptions)
        => new()
        {
            RequiredVersion = options.RequiredVersion,
            Metadata = metadata,
            RequiredState = commandOptions.RequiredState,
        };

    public static StreamWriteOptions GetStreamWriteOptions(
        this CommandRequestOptions requestOptions,
        CommandOptions commandOptions,
        StreamMetadata metadata)
        => commandOptions switch
        {
            { Consistency: CommandConsistency.ReadWrite }
                => new StreamWriteOptions()
                {
                    CorrelationId = requestOptions.CorrelationId,
                    CausationId = requestOptions.CommandId,
                    Metadata = metadata,
                    RequiredVersion = metadata.Version,
                },
            { Consistency: CommandConsistency.Write }
                => new StreamWriteOptions()
                {
                    CorrelationId = requestOptions.CorrelationId,
                    CausationId = requestOptions.CommandId,
                    Metadata = metadata,
                },
            _ => new StreamWriteOptions()
            {
                CorrelationId = Guid.NewGuid().ToString(),
                Metadata = metadata,
            },
        };
}
