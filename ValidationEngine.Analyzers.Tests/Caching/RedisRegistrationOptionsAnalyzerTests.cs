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
    public static class CacheManagerRegistrationExtensions
    {
        public static IServiceCollection AddRedisCacheManager(this IServiceCollection services, System.Action<CacheManagerOptions> configure)
            => services;
    }

    public sealed class CacheManagerOptions
    {
        public string HostName { get; set; } = string.Empty;
        public int Port { get; set; }
        public bool UseSsl { get; set; }
    }

    public class ServiceCollectionExtensionsHost
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
";

        private const string Postamble = @"
        }
    }
}
";

        private static CSharpAnalyzerTest<RedisRegistrationOptionsAnalyzer, DefaultVerifier> CreateTest(string body)
        {
            var test = CacheManagerTestSupport.Create<RedisRegistrationOptionsAnalyzer>(Preamble + body + Postamble, referenceCachingAbstractions: true);

            test.TestState.AdditionalReferences.Add(typeof(StackExchange.Redis.ConfigurationOptions).Assembly);
            test.TestState.AdditionalReferences.Add(typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection).Assembly);
            test.TestState.AdditionalReferences.Add(typeof(Microsoft.Extensions.Configuration.IConfiguration).Assembly);
            test.TestState.AdditionalReferences.Add(typeof(Microsoft.Extensions.Caching.StackExchangeRedis.RedisCacheOptions).Assembly);

            return test;
        }

        [Fact]
        public async Task WhenAddStackExchangeRedisCacheIsCalledWithOptionsThenCache002IsReported()
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
                    .WithLocation(0));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenAddStackExchangeRedisCacheIsFullyConfiguredThenCache002IsStillReported()
        {
            const string body = @"
            services.{|#0:AddStackExchangeRedisCache|}(options =>
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
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(RedisRegistrationOptionsAnalyzer.Rule)
                    .WithLocation(0));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenAddRedisCacheManagerIsUsedThenNoDiagnosticIsReported()
        {
            const string body = @"
            services.AddRedisCacheManager(options =>
            {
                options.HostName = configuration[""Redis:HostName""];
                options.Port = 6380;
                options.UseSsl = true;
            });
";

            var test = CreateTest(body);

            await test.RunAsync();
        }
    }
}
