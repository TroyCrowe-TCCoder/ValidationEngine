namespace ValidationEngine.Analyzers.Testing;

/// <summary>
/// Well-known type/attribute names used across the Testing analyzers.
/// </summary>
internal static class TestingWellKnownTypes
{
    public const string FactAttributeName = "FactAttribute";
    public const string TheoryAttributeName = "TheoryAttribute";
    public const string StopwatchMetadataName = "System.Diagnostics.Stopwatch";
    public const string BenchmarkAttributeName = "BenchmarkAttribute";
    public const string MoqMockNamespace = "Moq";
}
