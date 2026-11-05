using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Caching.Tests
{
    public class CacheLongLivedExpirationAnalyzerTests
    {
        private static CSharpAnalyzerTest<CacheLongLivedExpirationAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            var test = CacheManagerTestSupport.Create<CacheLongLivedExpirationAnalyzer>(source);
            test.TestState.Sources.Add(@"
namespace TestApp
{
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public sealed class LongLivedCacheAttribute : System.Attribute
    {
    }

    public class CacheOptions
    {
        public int LongLivedExpirationMinutes { get; init; }
    }
}
");

            return test;
        }

        [Fact]
        public async Task WhenLongLivedCacheMethodOmitsTtlThenCache016IsReported()
        {
            const string source = @"
using System.Threading.Tasks;
using RedisCacheManager.Caching;

namespace TestApp
{
    public class ItemTypeService
    {
        private readonly ICacheManager _cache;

        public ItemTypeService(ICacheManager cache)
        {
            _cache = cache;
        }

        [LongLivedCache]
        public async Task SetItemTypesAsync(string category, string id, string value)
        {
            await {|#0:_cache.SetAsync(category, id, value)|};
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(CacheLongLivedExpirationAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("SetItemTypesAsync", "does not pass a ttl"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenLongLivedCacheMethodPassesHardcodedTtlThenCache016IsReported()
        {
            const string source = @"
using System;
using System.Threading.Tasks;
using RedisCacheManager.Caching;

namespace TestApp
{
    public class ItemTypeService
    {
        private readonly ICacheManager _cache;

        public ItemTypeService(ICacheManager cache)
        {
            _cache = cache;
        }

        [LongLivedCache]
        public async Task SetItemTypesAsync(string category, string id, string value)
        {
            await _cache.SetAsync(category, id, value, {|#0:TimeSpan.FromHours(1)|});
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(CacheLongLivedExpirationAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("SetItemTypesAsync", "passes a ttl that is not CacheOptions.LongLivedExpirationMinutes"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenLongLivedCacheMethodPassesLongLivedExpirationMinutesThenNoDiagnosticIsReported()
        {
            const string source = @"
using System;
using System.Threading.Tasks;
using RedisCacheManager.Caching;

namespace TestApp
{
    public class ItemTypeService
    {
        private readonly ICacheManager _cache;
        private readonly CacheOptions _options = new CacheOptions();

        public ItemTypeService(ICacheManager cache)
        {
            _cache = cache;
        }

        [LongLivedCache]
        public async Task SetItemTypesAsync(string category, string id, string value)
        {
            await _cache.SetAsync(category, id, value, TimeSpan.FromMinutes(_options.LongLivedExpirationMinutes));
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenLongLivedCacheMethodPassesTtlViaLocalThenNoDiagnosticIsReported()
        {
            const string source = @"
using System;
using System.Threading.Tasks;
using RedisCacheManager.Caching;

namespace TestApp
{
    public class ItemTypeService
    {
        private readonly ICacheManager _cache;
        private readonly CacheOptions _options = new CacheOptions();

        public ItemTypeService(ICacheManager cache)
        {
            _cache = cache;
        }

        [LongLivedCache]
        public async Task SetItemTypesAsync(string category, string id, string value)
        {
            var ttl = TimeSpan.FromMinutes(_options.LongLivedExpirationMinutes);
            await _cache.SetAsync(category, id, value, ttl);
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenMethodIsNotMarkedLongLivedCacheThenNoDiagnosticIsReported()
        {
            const string source = @"
using System;
using System.Threading.Tasks;
using RedisCacheManager.Caching;

namespace TestApp
{
    public class UserProfileService
    {
        private readonly ICacheManager _cache;

        public UserProfileService(ICacheManager cache)
        {
            _cache = cache;
        }

        public async Task SetUserProfileAsync(string category, string id, string value)
        {
            await _cache.SetAsync(category, id, value, TimeSpan.FromMinutes(30));
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
