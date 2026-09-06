using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Security.Tests
{
    public class InMemoryTokenCacheAnalyzerTests
    {
        private static CSharpAnalyzerTest<InMemoryTokenCacheAnalyzer, DefaultVerifier> CreateTest(
            string source,
            string fileName)
        {
            var test = new CSharpAnalyzerTest<InMemoryTokenCacheAnalyzer, DefaultVerifier>
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.Sources.Add((fileName, source));

            return test;
        }

        private const string TokenCacheStub = @"
namespace TestApp
{
    public static class TokenCacheExtensions
    {
        public static void AddInMemoryTokenCaches(this object builder)
        {
        }

        public static void AddDistributedTokenCaches(this object builder)
        {
        }
    }
}
";

        [Fact]
        public async Task WhenAddInMemoryTokenCachesIsCalledThenSec004IsReported()
        {
            string source = TokenCacheStub + @"
namespace TestApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            object builder = null;
            {|#0:builder.AddInMemoryTokenCaches()|};
        }
    }
}
";

            var test = CreateTest(source, "Program.cs");
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(InMemoryTokenCacheAnalyzer.Rule)
                    .WithLocation(0));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenAddDistributedTokenCachesIsCalledThenNoDiagnosticIsReported()
        {
            string source = TokenCacheStub + @"
namespace TestApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            object builder = null;
            builder.AddDistributedTokenCaches();
        }
    }
}
";

            var test = CreateTest(source, "Program.cs");

            await test.RunAsync();
        }
    }
}
