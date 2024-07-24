using Chronicles.EventStore;

namespace Chronicles.Cqrs.Internal;

internal static class CommandExtensions
{
    public static StreamReadOptions GetStreamReadOptions(
        this CommandRequestOptions options,
        StreamMetadata metadata)
        => new()
        {
            RequiredVersion = options.RequiredVersion,
            Metadata = metadata,
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