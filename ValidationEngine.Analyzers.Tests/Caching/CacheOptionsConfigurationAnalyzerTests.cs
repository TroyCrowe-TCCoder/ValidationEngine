using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Caching.Tests
{
    public class CacheOptionsConfigurationAnalyzerTests
    {
        private static CSharpAnalyzerTest<CacheOptionsConfigurationAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            var test = new CSharpAnalyzerTest<CacheOptionsConfigurationAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.AdditionalReferences.Add(typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection).Assembly);
            test.TestState.AdditionalReferences.Add(typeof(Microsoft.Extensions.Options.IOptions<>).Assembly);

            return test;
        }

        [Fact]
        public async Task WhenCacheOptionsPropertyHasDefaultValueThenCache007IsReported()
        {
            const string source = @"
namespace TestApp
{
    public class {|#1:CacheOptions|}
    {
        public int UserProfileExpirationMinutes { get; init; } {|#0:= 30|};
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(CacheOptionsConfigurationAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("'CacheOptions.UserProfileExpirationMinutes' assigns a default value; all cache expiration values must come from configuration, not a default on the options class, per GlobalCachingStandards.md caching.6.4"));
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(CacheOptionsConfigurationAnalyzer.Rule)
                    .WithLocation(1)
                    .WithArguments("'CacheOptions' is never registered via Configure<CacheOptions>(...); strongly typed CacheOptions classes must be bound to configuration, per GlobalCachingStandards.md caching.6.4"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenCacheOptionsIsNeverRegisteredThenCache007IsReported()
        {
            const string source = @"
namespace TestApp
{
    public class {|#0:CacheOptions|}
    {
        public int UserProfileExpirationMinutes { get; init; }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(CacheOptionsConfigurationAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("'CacheOptions' is never registered via Configure<CacheOptions>(...); strongly typed CacheOptions classes must be bound to configuration, per GlobalCachingStandards.md caching.6.4"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenCacheOptionsHasNoDefaultsAndIsRegisteredThenNoDiagnosticIsReported()
        {
            const string source = @"
using Microsoft.Extensions.DependencyInjection;

namespace TestApp
{
    public class CacheOptions
    {
        public int UserProfileExpirationMinutes { get; init; }
    }

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CacheOptions>(options => { });
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
