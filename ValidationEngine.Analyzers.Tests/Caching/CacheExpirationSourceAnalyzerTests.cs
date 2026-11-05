using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Caching.Tests
{
    public class CacheExpirationSourceAnalyzerTests
    {
        private static CSharpAnalyzerTest<CacheExpirationSourceAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            var test = CacheManagerTestSupport.Create<CacheExpirationSourceAnalyzer>(source);
            test.TestState.AdditionalReferences.Add(typeof(Microsoft.Extensions.Options.IOptions<>).Assembly);

            return test;
        }

        [Fact]
        public async Task WhenIOptionsIsUsedInsteadOfIOptionsMonitorThenCache006IsReported()
        {
            const string source = @"
using Microsoft.Extensions.Options;

namespace TestApp
{
    public class CacheOptions
    {
        public int UserProfileExpirationMinutes { get; init; }
    }

    public class UserProfileService
    {
        private readonly object _cacheOptions;

        public UserProfileService({|#0:IOptions<CacheOptions>|} cacheOptions)
        {
            _cacheOptions = cacheOptions;
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(CacheExpirationSourceAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("'IOptions<CacheOptions>' uses IOptions<CacheOptions> instead of IOptionsMonitor<CacheOptions>; IOptions<T> is a startup snapshot and will not observe configuration changes, per GlobalCachingStandards.md caching.6.3"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenSetAsyncIsPassedAHardcodedTimeSpanThenCache006IsReported()
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

        public Task SaveAsync(string category, string id, string value)
        {
            return _cache.SetAsync(category, id, value, {|#0:TimeSpan.FromMinutes(30)|});
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(CacheExpirationSourceAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("SetAsync is passed a hardcoded 'TimeSpan.FromMinutes(30)' ttl instead of one sourced from IOptionsMonitor<CacheOptions>.CurrentValue, per GlobalCachingStandards.md caching.6.3"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenSetAsyncOmitsTtlThenCache006IsReported()
        {
            const string source = @"
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

        public Task SaveAsync(string category, string id, string value)
        {
            return {|#0:_cache.SetAsync(category, id, value)|};
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(CacheExpirationSourceAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("SetAsync does not pass a ttl and would rely on the package DefaultTtl; pass a ttl sourced from IOptionsMonitor<CacheOptions>.CurrentValue, per GlobalCachingStandards.md caching.6.1"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenTtlComesFromIOptionsMonitorCurrentValueThenNoDiagnosticIsReported()
        {
            const string source = @"
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using RedisCacheManager.Caching;

namespace TestApp
{
    public class CacheOptions
    {
        public int UserProfileExpirationMinutes { get; init; }
    }

    public class UserProfileService
    {
        private readonly ICacheManager _cache;
        private readonly IOptionsMonitor<CacheOptions> _cacheOptions;

        public UserProfileService(ICacheManager cache, IOptionsMonitor<CacheOptions> cacheOptions)
        {
            _cache = cache;
            _cacheOptions = cacheOptions;
        }

        public Task SaveAsync(string category, string id, string value)
        {
            return _cache.SetAsync(category, id, value, TimeSpan.FromMinutes(_cacheOptions.CurrentValue.UserProfileExpirationMinutes));
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
