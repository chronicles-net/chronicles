using System.Diagnostics.CodeAnalysis;
using Chronicles.EventStore;

namespace Chronicles.Tests.EventStore;

public class StreamIdTests
{
    [Theory, AutoNSubstituteData]
    public void Should_Be_Constructed_With_Id(
        [Frozen] string category,
        [Frozen] string id,
        StreamId sut)
        => ((string)sut)
            .Should()
            .Be($"{category}.{id}");

    [Theory, AutoNSubstituteData]
    [SuppressMessage("Usage", "xUnit1026:Theory methods should use all of their parameters", Justification = "Needed by test")]
    public void Should_Be_EqualTo(
        [Frozen] string category, // The same id will be injected into both left and right.
        [Frozen] string id, // The same id will be injected into both left and right.
        StreamId left,
        StreamId right)
        => (left == right)
            .Should()
            .BeTrue();

    [Theory, AutoNSubstituteData]
    public void Should_Support_Explicit_String_Overload(
        [Frozen] string category,
        [Frozen] string id,
        StreamId sut)
        => ((string)sut)
            .Should()
            .Be($"{category}.{id}");

    [Theory, AutoNSubstituteData]
    public void Should_Support_Getting_StreamId_FromString(
        [Frozen] string category,
        [Frozen] string id,
        StreamId sut)
        => StreamId
            .FromString((string)sut)
            .Should()
            .BeEquivalentTo(
                new StreamId(category, id));
}