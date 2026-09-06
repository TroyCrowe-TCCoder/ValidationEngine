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
            var test = new CSharpAnalyzerTest<CacheExpirationSourceAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.AdditionalReferences.Add(typeof(Microsoft.Extensions.Caching.Distributed.IDistributedCache).Assembly);
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
        public async Task WhenSetAbsoluteExpirationIsPassedAHardcodedTimeSpanThenCache006IsReported()
        {
            const string source = @"
using System;
using Microsoft.Extensions.Caching.Distributed;

namespace TestApp
{
    public class UserProfileService
    {
        public void ConfigureExpiration()
        {
            var options = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration({|#0:TimeSpan.FromMinutes(30)|});
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(CacheExpirationSourceAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("SetAbsoluteExpiration is passed a hardcoded 'TimeSpan.FromMinutes(30)' value instead of an expiration sourced from IOptionsMonitor<CacheOptions>.CurrentValue, per GlobalCachingStandards.md caching.6.3"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenExpirationComesFromIOptionsMonitorCurrentValueThenNoDiagnosticIsReported()
        {
            const string source = @"
using System;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace TestApp
{
    public class CacheOptions
    {
        public int UserProfileExpirationMinutes { get; init; }
    }

    public class UserProfileService
    {
        private readonly IOptionsMonitor<CacheOptions> _cacheOptions;

        public UserProfileService(IOptionsMonitor<CacheOptions> cacheOptions)
        {
            _cacheOptions = cacheOptions;
        }

        public void ConfigureExpiration()
        {
            var options = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(
                    TimeSpan.FromMinutes(_cacheOptions.CurrentValue.UserProfileExpirationMinutes));
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
