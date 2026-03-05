using Chronicles.EventStore;
using NSubstitute;

namespace Chronicles.Tests.EventStore;

public class StreamMetadataExtensionsTests
{
    [Theory, AutoNSubstituteData]
    public void EnsureSuccess_Should_Return_Metadata_When_No_Constraints(
        StreamId streamId,
        StreamVersion version)
    {
        var metadata = Substitute.For<StreamMetadata>(streamId, StreamState.Active, version, DateTimeOffset.UtcNow);

        var result = metadata.EnsureSuccess(null);

        result.Should().BeSameAs(metadata);
    }

    [Theory, AutoNSubstituteData]
    public void EnsureSuccess_Should_Return_Metadata_When_RequiredState_Matches(
        StreamId streamId,
        StreamVersion version)
    {
        var metadata = Substitute.For<StreamMetadata>(streamId, StreamState.Active, version, DateTimeOffset.UtcNow);
        var options = new TestStreamOptions { RequiredState = StreamState.Active };

        var result = metadata.EnsureSuccess(options);

        result.Should().BeSameAs(metadata);
    }

    [Theory, AutoNSubstituteData]
    public void EnsureSuccess_Should_Throw_When_RequiredState_Does_Not_Match(
        StreamId streamId,
        StreamVersion version)
    {
        var metadata = Substitute.For<StreamMetadata>(streamId, StreamState.Active, version, DateTimeOffset.UtcNow);
        var options = new TestStreamOptions { RequiredState = StreamState.Closed };

        var act = () => metadata.EnsureSuccess(options);

        act.Should().Throw<StreamConflictException>()
            .WithMessage("Expecting the stream state to be Closed, but found Active.");
    }

    [Theory, AutoNSubstituteData]
    public void EnsureSuccess_Should_Return_Metadata_When_RequiredVersion_Matches(
        StreamId streamId)
    {
        var version = new StreamVersion(42);
        var metadata = Substitute.For<StreamMetadata>(streamId, StreamState.Active, version, DateTimeOffset.UtcNow);
        var options = new TestStreamOptions { RequiredVersion = new StreamVersion(42) };

        var result = metadata.EnsureSuccess(options);

        result.Should().BeSameAs(metadata);
    }

    [Theory, AutoNSubstituteData]
    public void EnsureSuccess_Should_Throw_When_RequiredVersion_Does_Not_Match(
        StreamId streamId)
    {
        var version = new StreamVersion(42);
        var metadata = Substitute.For<StreamMetadata>(streamId, StreamState.Active, version, DateTimeOffset.UtcNow);
        var options = new TestStreamOptions { RequiredVersion = new StreamVersion(100) };

        var act = () => metadata.EnsureSuccess(options);

        act.Should().Throw<StreamConflictException>()
            .WithMessage("Stream is not at the required version.");
    }

    [Fact]
    public void EnsureSuccess_With_RequireEmpty_Should_Pass_For_Empty_Stream()
    {
        var streamId = new StreamId("test", "id");
        var emptyVersion = new StreamVersion(0);
        var metadata = Substitute.For<StreamMetadata>(streamId, StreamState.New, emptyVersion, DateTimeOffset.UtcNow);
        var options = new TestStreamOptions { RequiredVersion = StreamVersion.RequireEmpty };

        var result = metadata.EnsureSuccess(options);

        result.Should().BeSameAs(metadata);
    }

    [Fact]
    public void EnsureSuccess_With_RequireEmpty_Should_Throw_For_Non_Empty_Stream()
    {
        var streamId = new StreamId("test", "id");
        var nonEmptyVersion = new StreamVersion(5);
        var metadata = Substitute.For<StreamMetadata>(streamId, StreamState.Active, nonEmptyVersion, DateTimeOffset.UtcNow);
        var options = new TestStreamOptions { RequiredVersion = StreamVersion.RequireEmpty };

        var act = () => metadata.EnsureSuccess(options);

        act.Should().Throw<StreamConflictException>()
            .WithMessage("Stream is not at the required version.");
    }

    [Fact]
    public void EnsureSuccess_With_RequireNotEmpty_Should_Pass_For_Non_Empty_Stream()
    {
        var streamId = new StreamId("test", "id");
        var nonEmptyVersion = new StreamVersion(5);
        var metadata = Substitute.For<StreamMetadata>(streamId, StreamState.Active, nonEmptyVersion, DateTimeOffset.UtcNow);
        var options = new TestStreamOptions { RequiredVersion = StreamVersion.RequireNotEmpty };

        var result = metadata.EnsureSuccess(options);

        result.Should().BeSameAs(metadata);
    }

    [Fact]
    public void EnsureSuccess_With_RequireNotEmpty_Should_Throw_For_Empty_Stream()
    {
        var streamId = new StreamId("test", "id");
        var emptyVersion = new StreamVersion(0);
        var metadata = Substitute.For<StreamMetadata>(streamId, StreamState.New, emptyVersion, DateTimeOffset.UtcNow);
        var options = new TestStreamOptions { RequiredVersion = StreamVersion.RequireNotEmpty };

        var act = () => metadata.EnsureSuccess(options);

        act.Should().Throw<StreamConflictException>()
            .WithMessage("Stream is not at the required version.");
    }

    [Fact]
    public void EnsureSuccess_With_Any_Should_Pass_For_Empty_Stream()
    {
        var streamId = new StreamId("test", "id");
        var emptyVersion = new StreamVersion(0);
        var metadata = Substitute.For<StreamMetadata>(streamId, StreamState.New, emptyVersion, DateTimeOffset.UtcNow);
        var options = new TestStreamOptions { RequiredVersion = StreamVersion.Any };

        var result = metadata.EnsureSuccess(options);

        result.Should().BeSameAs(metadata);
    }

    [Fact]
    public void EnsureSuccess_With_Any_Should_Pass_For_Non_Empty_Stream()
    {
        var streamId = new StreamId("test", "id");
        var nonEmptyVersion = new StreamVersion(5);
        var metadata = Substitute.For<StreamMetadata>(streamId, StreamState.Active, nonEmptyVersion, DateTimeOffset.UtcNow);
        var options = new TestStreamOptions { RequiredVersion = StreamVersion.Any };

        var result = metadata.EnsureSuccess(options);

        result.Should().BeSameAs(metadata);
    }

    [Theory, AutoNSubstituteData]
    public void EnsureSuccess_Should_Check_State_Before_Version(
        StreamId streamId)
    {
        var metadata = Substitute.For<StreamMetadata>(streamId, StreamState.Active, new StreamVersion(5), DateTimeOffset.UtcNow);
        var options = new TestStreamOptions
        {
            RequiredState = StreamState.Closed,
            RequiredVersion = new StreamVersion(5)
        };

        var act = () => metadata.EnsureSuccess(options);

        act.Should().Throw<StreamConflictException>()
            .WithMessage("Expecting the stream state to be Closed, but found Active.");
    }

    private sealed class TestStreamOptions : StreamOptions
    {
    }
}
