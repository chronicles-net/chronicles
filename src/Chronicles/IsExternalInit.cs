// Explicit architectural exception (approved: Lars Skovslund, 2026-03-05):
// The Chronicles.Tests assembly is granted access to all internal types across all
// layers (Documents.Internal, EventStore.Internal, Cqrs.Internal) for test fakes
// and integration testing. This is the only permitted consumer of cross-layer internals.
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Chronicles.Tests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace System.Runtime.CompilerServices;

using System.ComponentModel;

/// <summary>
/// Reserved to be used by the compiler for tracking metadata.
/// This class should not be used by developers in source code.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
internal static class IsExternalInit
{
}