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
            var test = CacheManagerTestSupport.Create<CacheObservabilityAnalyzer>(source);
            test.TestState.AdditionalReferences.Add(typeof(Microsoft.Extensions.Logging.ILogger<>).Assembly);

            return test;
        }

        [Fact]
        public async Task WhenCacheHitIsLoggedAroundICacheManagerCallThenCache015IsReported()
        {
            const string source = @"
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RedisCacheManager.Caching;

namespace TestApp
{
    public class UserProfileService
    {
        private readonly ICacheManager _cache;
        private readonly ILogger<UserProfileService> _logger;

        public UserProfileService(ICacheManager cache, ILogger<UserProfileService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<string?> GetUserProfileAsync(string category, string id)
        {
            var (found, value) = await _cache.TryGetAsync<string>(category, id);

            if (found)
            {
                {|#0:_logger.LogDebug(""Cache hit for {Id}"", id)|};
            }

            return value;
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(CacheObservabilityAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("GetUserProfileAsync"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenCacheMissIsLoggedAroundICacheManagerCallThenCache015IsReported()
        {
            const string source = @"
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RedisCacheManager.Caching;

namespace TestApp
{
    public class UserProfileService
    {
        private readonly ICacheManager _cache;
        private readonly ILogger<UserProfileService> _logger;

        public UserProfileService(ICacheManager cache, ILogger<UserProfileService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<string?> GetUserProfileAsync(string category, string id)
        {
            var (found, value) = await _cache.TryGetAsync<string>(category, id);

            if (!found)
            {
                {|#0:_logger.LogInformation($""cache miss for {id}"")|};
            }

            return value;
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(CacheObservabilityAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("GetUserProfileAsync"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenLoggingIsUnrelatedToHitOrMissThenNoDiagnosticIsReported()
        {
            const string source = @"
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RedisCacheManager.Caching;

namespace TestApp
{
    public class UserProfileService
    {
        private readonly ICacheManager _cache;
        private readonly ILogger<UserProfileService> _logger;

        public UserProfileService(ICacheManager cache, ILogger<UserProfileService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<string?> GetUserProfileAsync(string category, string id)
        {
            _logger.LogInformation(""Loading profile {Id}"", id);

            var (_, value) = await _cache.TryGetAsync<string>(category, id);

            return value;
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenMethodDoesNotCallICacheManagerThenNoDiagnosticIsReported()
        {
            const string source = @"
using Microsoft.Extensions.Logging;

namespace TestApp
{
    public class UserProfileService
    {
        private readonly ILogger<UserProfileService> _logger;

        public UserProfileService(ILogger<UserProfileService> logger)
        {
            _logger = logger;
        }

        public void Report()
        {
            _logger.LogDebug(""Cache hit statistics were requested"");
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
