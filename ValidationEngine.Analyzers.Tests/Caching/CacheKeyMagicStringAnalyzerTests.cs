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
            return CacheManagerTestSupport.Create<CacheKeyMagicStringAnalyzer>(source);
        }

        [Fact]
        public async Task WhenCategoryAndIdAreStringLiteralsThenCache004IsReportedForEach()
        {
            const string source = @"
using System.Threading.Tasks;
using RedisCacheManager.Caching;

namespace TestApp
{
    public class ReferenceDataService
    {
        private readonly ICacheManager _cache;

        public ReferenceDataService(ICacheManager cache)
        {
            _cache = cache;
        }

        public Task<(bool Found, string? Value)> LoadAsync()
        {
            return _cache.TryGetAsync<string>({|#0:""ReferenceData""|}, {|#1:""client-reference-data""|});
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(CacheKeyMagicStringAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("TryGetAsync", "category"));
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(CacheKeyMagicStringAnalyzer.Rule)
                    .WithLocation(1)
                    .WithArguments("TryGetAsync", "id"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenIdIsInterpolatedStringThenCache004IsReported()
        {
            const string source = @"
using System.Threading.Tasks;
using RedisCacheManager.Caching;

namespace TestApp
{
    public static class CacheKeys
    {
        public const string ReferenceData = ""ReferenceData"";
    }

    public class ReferenceDataService
    {
        private readonly ICacheManager _cache;

        public ReferenceDataService(ICacheManager cache)
        {
            _cache = cache;
        }

        public Task RemoveAsync(string tenantId)
        {
            return _cache.RemoveAsync(CacheKeys.ReferenceData, {|#0:$""{tenantId}:client-reference-data""|});
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(CacheKeyMagicStringAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("RemoveAsync", "id"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenSetAsyncIdIsConcatenatedThenCache004IsReported()
        {
            const string source = @"
using System;
using System.Threading.Tasks;
using RedisCacheManager.Caching;

namespace TestApp
{
    public static class CacheKeys
    {
        public const string ReferenceData = ""ReferenceData"";
    }

    public class ReferenceDataService
    {
        private readonly ICacheManager _cache;

        public ReferenceDataService(ICacheManager cache)
        {
            _cache = cache;
        }

        public Task SaveAsync(string tenantId, string value, TimeSpan ttl)
        {
            return _cache.SetAsync(CacheKeys.ReferenceData, {|#0:tenantId + "":data""|}, value, ttl);
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(CacheKeyMagicStringAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("SetAsync", "id"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenCategoryAndIdComeFromCacheKeysClassThenNoDiagnosticIsReported()
        {
            const string source = @"
using System.Threading.Tasks;
using RedisCacheManager.Caching;

namespace TestApp
{
    public static class CacheKeys
    {
        public const string ReferenceData = ""ReferenceData"";

        public static string ClientReferenceData(string tenantId) => $""{tenantId}:client-reference-data"";
    }

    public class ReferenceDataService
    {
        private readonly ICacheManager _cache;

        public ReferenceDataService(ICacheManager cache)
        {
            _cache = cache;
        }

        public Task<(bool Found, string? Value)> LoadAsync(string tenantId)
        {
            return _cache.TryGetAsync<string>(CacheKeys.ReferenceData, CacheKeys.ClientReferenceData(tenantId));
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
