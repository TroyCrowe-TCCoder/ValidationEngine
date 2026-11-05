using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Caching.Tests
{
    public class CacheAsideResilienceAnalyzerTests
    {
        private static CSharpAnalyzerTest<CacheAsideResilienceAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            return CacheManagerTestSupport.Create<CacheAsideResilienceAnalyzer>(source);
        }

        [Fact]
        public async Task WhenTryGetAsyncIsWrappedInTryCatchThenCache014IsReported()
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

        public async Task<string?> GetUserProfileAsync(string category, string id)
        {
            try
            {
                var (found, value) = await {|#0:_cache.TryGetAsync<string>(category, id)|};
                return found ? value : null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(CacheAsideResilienceAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("GetUserProfileAsync", "TryGetAsync"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenSetAsyncIsWrappedInTryCatchThenCache014IsReported()
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

        public async Task SaveAsync(string category, string id, string value, TimeSpan ttl)
        {
            try
            {
                await {|#0:_cache.SetAsync(category, id, value, ttl)|};
            }
            catch (Exception)
            {
            }
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(CacheAsideResilienceAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("SaveAsync", "SetAsync"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenCacheCallIsNotWrappedInTryCatchThenNoDiagnosticIsReported()
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

        public async Task<string?> GetUserProfileAsync(string category, string id)
        {
            var (found, value) = await _cache.TryGetAsync<string>(category, id);
            return found ? value : null;
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenTryHasOnlyFinallyThenNoDiagnosticIsReported()
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

        public async Task RemoveAsync(string category, string id)
        {
            try
            {
                await _cache.RemoveAsync(category, id);
            }
            finally
            {
            }
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenCatchWrapsANonCacheCallThenNoDiagnosticIsReported()
        {
            const string source = @"
using System;
using System.Threading.Tasks;

namespace TestApp
{
    public class UserProfileService
    {
        public Task<int> ParseAsync(string text)
        {
            try
            {
                return Task.FromResult(int.Parse(text));
            }
            catch (FormatException)
            {
                return Task.FromResult(0);
            }
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
