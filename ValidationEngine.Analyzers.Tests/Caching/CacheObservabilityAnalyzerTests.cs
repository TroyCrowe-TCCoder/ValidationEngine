using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Caching.Tests
{
    public class CacheObservabilityAnalyzerTests
    {
        private static CSharpAnalyzerTest<CacheObservabilityAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            var test = new CSharpAnalyzerTest<CacheObservabilityAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.AdditionalReferences.Add(typeof(Microsoft.Extensions.Caching.Distributed.IDistributedCache).Assembly);
            test.TestState.AdditionalReferences.Add(typeof(Microsoft.Extensions.Logging.ILogger<>).Assembly);

            return test;
        }

        [Fact]
        public async Task WhenCacheCallHasNoLoggingThenCache015IsReported()
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
            return await _cache.{|#0:GetAsync|}(key);
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(CacheObservabilityAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("GetUserProfileAsync", "GetAsync"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenCacheCallIsAccompaniedByLoggingThenNoDiagnosticIsReported()
        {
            const string source = @"
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace TestApp
{
    public class UserProfileService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<UserProfileService> _logger;

        public UserProfileService(IDistributedCache cache, ILogger<UserProfileService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<byte[]?> GetUserProfileAsync(string key)
        {
            var result = await _cache.GetAsync(key);

            if (result is null)
            {
                _logger.LogDebug(""Cache miss for {Key}"", key);
            }
            else
            {
                _logger.LogDebug(""Cache hit for {Key}"", key);
            }

            return result;
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenMethodDoesNotCallDistributedCacheThenNoDiagnosticIsReported()
        {
            const string source = @"
using System.Threading.Tasks;

namespace TestApp
{
    public class UserProfileService
    {
        public Task DoWorkAsync()
        {
            return Task.CompletedTask;
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
