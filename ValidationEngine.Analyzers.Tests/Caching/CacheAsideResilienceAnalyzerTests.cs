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
            var test = new CSharpAnalyzerTest<CacheAsideResilienceAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.AdditionalReferences.Add(typeof(Microsoft.Extensions.Caching.Distributed.IDistributedCache).Assembly);

            return test;
        }

        [Fact]
        public async Task WhenGetAsyncCallHasNoSurroundingTryCatchThenCache014IsReported()
        {
            const string source = @"
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace TestApp
{
    public class UserProfileService
    {
        private readonly IDistributedCache _cache;

        public UserProfileService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<byte[]?> GetUserProfileAsync(string key)
        {
            return await {|#0:_cache.GetAsync(key)|};
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(CacheAsideResilienceAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("GetUserProfileAsync", "GetAsync"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenGetAsyncCallIsInTryCatchThatFallsThroughThenNoDiagnosticIsReported()
        {
            const string source = @"
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace TestApp
{
    public class UserProfileService
    {
        private readonly IDistributedCache _cache;

        public UserProfileService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<byte[]?> GetUserProfileAsync(string key)
        {
            try
            {
                return await _cache.GetAsync(key);
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

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenCatchClauseRethrowsThenCache014IsReported()
        {
            const string source = @"
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace TestApp
{
    public class UserProfileService
    {
        private readonly IDistributedCache _cache;

        public UserProfileService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<byte[]?> GetUserProfileAsync(string key)
        {
            try
            {
                return await {|#0:_cache.GetAsync(key)|};
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(CacheAsideResilienceAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("GetUserProfileAsync", "GetAsync"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenSetAsyncCallIsInTryCatchThatFallsThroughThenNoDiagnosticIsReported()
        {
            const string source = @"
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace TestApp
{
    public class UserProfileService
    {
        private readonly IDistributedCache _cache;

        public UserProfileService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task SetUserProfileAsync(string key, byte[] data)
        {
            try
            {
                await _cache.SetAsync(key, data);
            }
            catch (Exception)
            {
                // logged elsewhere; fall through
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
