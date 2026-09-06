using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Caching.Tests
{
    public class CacheKeyMagicStringAnalyzerTests
    {
        private static CSharpAnalyzerTest<CacheKeyMagicStringAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            var test = new CSharpAnalyzerTest<CacheKeyMagicStringAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.AdditionalReferences.Add(typeof(Microsoft.Extensions.Caching.Distributed.IDistributedCache).Assembly);

            return test;
        }

        [Fact]
        public async Task WhenCacheKeyIsStringLiteralThenCache004IsReported()
        {
            const string source = @"
using System.Threading.Tasks;
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

        public Task<byte[]> LoadAsync()
        {
            return _cache.GetAsync({|#0:""client-reference-data""|});
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(CacheKeyMagicStringAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("GetAsync"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenCacheKeyIsInterpolatedStringThenCache004IsReported()
        {
            const string source = @"
using System.Threading.Tasks;
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

        public Task<byte[]> LoadAsync(string tenantId)
        {
            return _cache.GetAsync({|#0:$""{tenantId}:client-reference-data""|});
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(CacheKeyMagicStringAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("GetAsync"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenCacheKeyComesFromCacheKeysClassThenNoDiagnosticIsReported()
        {
            const string source = @"
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace TestApp
{
    public static class CacheKeys
    {
        public static string ClientReferenceData(string tenantId) => $""{tenantId}:client-reference-data"";
    }

    public class ReferenceDataService
    {
        private readonly IDistributedCache _cache;

        public ReferenceDataService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public Task<byte[]> LoadAsync(string tenantId)
        {
            return _cache.GetAsync(CacheKeys.ClientReferenceData(tenantId));
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
