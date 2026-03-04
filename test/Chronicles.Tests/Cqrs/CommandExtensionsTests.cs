using Chronicles.Cqrs;
using Chronicles.Cqrs.Internal;
using Chronicles.EventStore;

namespace Chronicles.Tests.Cqrs;

public class CommandExtensionsTests
{
    [Theory, AutoNSubstituteData]
    public void GetStreamReadOptions_Should_Include_RequiredVersion_From_RequestOptions(
        StreamMetadata metadata)
    {
        var requestOptions = new CommandRequestOptions { RequiredVersion = new StreamVersion(5) };
        var commandOptions = new CommandOptions();

        var result = requestOptions.GetStreamReadOptions(metadata, commandOptions);

        result.RequiredVersion.Should().Be(new StreamVersion(5));
    }

    [Theory, AutoNSubstituteData]
    public void GetStreamReadOptions_Should_Include_Metadata(
        StreamMetadata metadata)
    {
        var requestOptions = new CommandRequestOptions();
        var commandOptions = new CommandOptions();

        var result = requestOptions.GetStreamReadOptions(metadata, commandOptions);

        result.Metadata.Should().BeSameAs(metadata);
    }

    [Theory, AutoNSubstituteData]
    public void GetStreamReadOptions_Should_Include_RequiredState_From_CommandOptions(
        StreamMetadata metadata)
    {
        var requestOptions = new CommandRequestOptions();
        var commandOptions = new CommandOptions { RequiredState = StreamState.Active };

        var result = requestOptions.GetStreamReadOptions(metadata, commandOptions);

        result.RequiredState.Should().Be(StreamState.Active);
    }

    [Theory, AutoNSubstituteData]
    public void GetStreamReadOptions_Without_RequiredState_Should_Return_Null_RequiredState(
        StreamMetadata metadata)
    {
        var requestOptions = new CommandRequestOptions();
        var commandOptions = new CommandOptions { RequiredState = null };

        var result = requestOptions.GetStreamReadOptions(metadata, commandOptions);

        result.RequiredState.Should().BeNull();
    }

    [Theory, AutoNSubstituteData]
    public void GetStreamWriteOptions_With_ReadWrite_Consistency_Should_Include_Stream_Version(
        StreamId streamId)
    {
        var version = new StreamVersion(7);
        var metadata = Substitute.For<StreamMetadata>(streamId, StreamState.Active, version, DateTimeOffset.UtcNow);
        var requestOptions = new CommandRequestOptions
        {
            CommandId = "cmd-1",
            CorrelationId = "corr-1",
        };
        var commandOptions = new CommandOptions { Consistency = CommandConsistency.ReadWrite };

        var result = requestOptions.GetStreamWriteOptions(commandOptions, metadata);

        result.RequiredVersion.Should().Be(version);
    }

    [Theory, AutoNSubstituteData]
    public void GetStreamWriteOptions_With_ReadWrite_Consistency_Should_Include_CorrelationId_And_CausationId(
        StreamId streamId)
    {
        var metadata = Substitute.For<StreamMetadata>(streamId, StreamState.Active, new StreamVersion(3), DateTimeOffset.UtcNow);
        var requestOptions = new CommandRequestOptions
        {
            CommandId = "cmd-42",
            CorrelationId = "corr-42",
        };
        var commandOptions = new CommandOptions { Consistency = CommandConsistency.ReadWrite };

        var result = requestOptions.GetStreamWriteOptions(commandOptions, metadata);

        result.CorrelationId.Should().Be("corr-42");
        result.CausationId.Should().Be("cmd-42");
    }

    [Theory, AutoNSubstituteData]
    public void GetStreamWriteOptions_With_ReadWrite_Consistency_Should_Include_Metadata(
        StreamId streamId)
    {
        var metadata = Substitute.For<StreamMetadata>(streamId, StreamState.Active, new StreamVersion(3), DateTimeOffset.UtcNow);
        var requestOptions = new CommandRequestOptions();
        var commandOptions = new CommandOptions { Consistency = CommandConsistency.ReadWrite };

        var result = requestOptions.GetStreamWriteOptions(commandOptions, metadata);

        result.Metadata.Should().BeSameAs(metadata);
    }

    [Theory, AutoNSubstituteData]
    public void GetStreamWriteOptions_With_Write_Consistency_Should_Not_Include_RequiredVersion(
        StreamId streamId)
    {
        var metadata = Substitute.For<StreamMetadata>(streamId, StreamState.Active, new StreamVersion(7), DateTimeOffset.UtcNow);
        var requestOptions = new CommandRequestOptions
        {
            CommandId = "cmd-1",
            CorrelationId = "corr-1",
        };
        var commandOptions = new CommandOptions { Consistency = CommandConsistency.Write };

        var result = requestOptions.GetStreamWriteOptions(commandOptions, metadata);

        result.RequiredVersion.Should().BeNull();
    }

    [Theory, AutoNSubstituteData]
    public void GetStreamWriteOptions_With_Write_Consistency_Should_Include_CorrelationId_And_CausationId(
        StreamId streamId)
    {
        var metadata = Substitute.For<StreamMetadata>(streamId, StreamState.Active, new StreamVersion(3), DateTimeOffset.UtcNow);
        var requestOptions = new CommandRequestOptions
        {
            CommandId = "cmd-99",
            CorrelationId = "corr-99",
        };
        var commandOptions = new CommandOptions { Consistency = CommandConsistency.Write };

        var result = requestOptions.GetStreamWriteOptions(commandOptions, metadata);

        result.CorrelationId.Should().Be("corr-99");
        result.CausationId.Should().Be("cmd-99");
    }
}
