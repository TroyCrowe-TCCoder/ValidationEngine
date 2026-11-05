using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Caching.Tests
{
    public class NoDistributedCacheAnalyzerTests
    {
        private static CSharpAnalyzerTest<NoDistributedCacheAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            return CacheManagerTestSupport.Create<NoDistributedCacheAnalyzer>(source, referenceCachingAbstractions: true);
        }

        [Fact]
        public async Task WhenFieldIsTypeIDistributedCacheThenCache017IsReported()
        {
            const string source = @"
using Microsoft.Extensions.Caching.Distributed;

namespace TestApp
{
    public class ExpenseService
    {
        private readonly {|#0:IDistributedCache|} _cache = null!;
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(NoDistributedCacheAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("_cache"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenConstructorParameterIsTypeIDistributedCacheThenCache017IsReported()
        {
            const string source = @"
using Microsoft.Extensions.Caching.Distributed;

namespace TestApp
{
    public class ExpenseService
    {
        public ExpenseService({|#0:IDistributedCache|} cache)
        {
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(NoDistributedCacheAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("cache"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenServiceUsesICacheManagerThenNoDiagnosticIsReported()
        {
            const string source = @"
using RedisCacheManager.Caching;

namespace TestApp
{
    public class ExpenseService
    {
        private readonly ICacheManager _cache;

        public ExpenseService(ICacheManager cache)
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
