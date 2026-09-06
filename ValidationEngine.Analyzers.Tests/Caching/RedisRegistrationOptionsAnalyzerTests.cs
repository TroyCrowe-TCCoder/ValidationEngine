using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Caching.Tests
{
    public class RedisRegistrationOptionsAnalyzerTests
    {
        private const string Preamble = @"
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TestApp
{
    public class ServiceCollectionExtensionsHost
    {
        public void ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
";

        private const string Postamble = @"
        }
    }
}
";

        private static CSharpAnalyzerTest<RedisRegistrationOptionsAnalyzer, DefaultVerifier> CreateTest(string body)
        {
            var source = Preamble + body + Postamble;

            var test = new CSharpAnalyzerTest<RedisRegistrationOptionsAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.AdditionalReferences.Add(typeof(Microsoft.Extensions.Caching.Distributed.IDistributedCache).Assembly);
            test.TestState.AdditionalReferences.Add(typeof(StackExchange.Redis.ConfigurationOptions).Assembly);
            test.TestState.AdditionalReferences.Add(typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection).Assembly);
            test.TestState.AdditionalReferences.Add(typeof(Microsoft.Extensions.Configuration.IConfiguration).Assembly);
            test.TestState.AdditionalReferences.Add(typeof(Microsoft.Extensions.Caching.StackExchangeRedis.RedisCacheOptions).Assembly);

            return test;
        }

        [Fact]
        public async Task WhenRegistrationUsesHardcodedConnectionStringThenCache002IsReported()
        {
            const string body = @"
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = {|#0:""localhost:6379""|};
                options.ConfigurationOptions = new StackExchange.Redis.ConfigurationOptions
                {
                    AbortOnConnectFail = false
                };
            });
";

            var test = CreateTest(body);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(RedisRegistrationOptionsAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("AddStackExchangeRedisCache uses a hardcoded connection string; source it from configuration (e.g. Key Vault-backed), per GlobalCachingStandards.md caching.3.1"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenRegistrationDoesNotSetAbortOnConnectFailThenCache002IsReported()
        {
            const string body = @"
            services.{|#0:AddStackExchangeRedisCache|}(options =>
            {
                options.Configuration = configuration.GetConnectionString(""Redis"");
            });
";

            var test = CreateTest(body);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(RedisRegistrationOptionsAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("AddStackExchangeRedisCache registration does not set AbortOnConnectFail; the app must not crash on startup if Redis is temporarily unavailable, per GlobalCachingStandards.md caching.3.1"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenRegistrationIsFullyCompliantThenNoDiagnosticIsReported()
        {
            const string body = @"
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration.GetConnectionString(""Redis"");
                options.InstanceName = configuration[""Cache:InstanceName""];
                options.ConfigurationOptions = new StackExchange.Redis.ConfigurationOptions
                {
                    ConnectRetry = 3,
                    AbortOnConnectFail = false
                };
            });
";

            var test = CreateTest(body);

            await test.RunAsync();
        }
    }
}
