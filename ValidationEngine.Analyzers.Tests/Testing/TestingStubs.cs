namespace ValidationEngine.Analyzers.Testing.Tests;

internal static class TestingStubs
{
    public const string XunitStub = @"
namespace Xunit
{
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public class FactAttribute : System.Attribute { }

    [System.AttributeUsage(System.AttributeTargets.Method)]
    public class TheoryAttribute : System.Attribute { }
}
";

    public const string BenchmarkDotNetStub = @"
namespace BenchmarkDotNet.Attributes
{
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public class BenchmarkAttribute : System.Attribute { }
}
";

    public const string MoqStub = @"
namespace Moq
{
    public class Mock<T> where T : class
    {
        public T Object => default!;
    }
}
";
}
