using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Caching.Tests
{
    public class NoMemoryCacheAnalyzerTests
    {
        private static CSharpAnalyzerTest<NoMemoryCacheAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            var test = new CSharpAnalyzerTest<NoMemoryCacheAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            // IMemoryCache and IDistributedCache both live in Microsoft.Extensions.Caching.Abstractions.dll;
            // add the assembly once to avoid a duplicate metadata reference.
            test.TestState.AdditionalReferences.Add(typeof(Microsoft.Extensions.Caching.Memory.IMemoryCache).Assembly);

            return test;
        }

        [Fact]
        public async Task WhenFieldIsTypeIMemoryCacheThenCache001IsReported()
        {
            const string source = @"
using Microsoft.Extensions.Caching.Memory;

namespace TestApp
{
    public class ExpenseService
    {
        private readonly {|#0:IMemoryCache|} _cache;
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(NoMemoryCacheAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("_cache"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenFieldIsTypeIDistributedCacheThenNoDiagnosticIsReported()
        {
            const string source = @"
using Microsoft.Extensions.Caching.Distributed;

namespace TestApp
{
    public class ExpenseService
    {
        private readonly IDistributedCache _cache;

        public ExpenseService(IDistributedCache cache)
        {
            _cache = cache;
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
