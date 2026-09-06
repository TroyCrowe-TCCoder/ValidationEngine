using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Caching.Tests
{
    public class NoDirectRedisClientAnalyzerTests
    {
        private static CSharpAnalyzerTest<NoDirectRedisClientAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            var test = new CSharpAnalyzerTest<NoDirectRedisClientAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.AdditionalReferences.Add(typeof(StackExchange.Redis.ConnectionMultiplexer).Assembly);
            test.TestState.AdditionalReferences.Add(typeof(Microsoft.Extensions.Caching.Distributed.IDistributedCache).Assembly);

            return test;
        }

        [Fact]
        public async Task WhenFieldIsTypeIConnectionMultiplexerThenCache003IsReported()
        {
            const string source = @"
using StackExchange.Redis;

namespace TestApp
{
    public class ReferenceDataService
    {
        private readonly {|#0:IConnectionMultiplexer|} _redis;
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(NoDirectRedisClientAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("_redis", "IConnectionMultiplexer"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenFieldIsTypeIDistributedCacheThenNoDiagnosticIsReported()
        {
            const string source = @"
using Microsoft.Extensions.Caching.Distributed;

namespace TestApp
{
    public class ReferenceDataService
    {
        private readonly IDistributedCache _cache;

        public ReferenceDataService(IDistributedCache cache)
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
