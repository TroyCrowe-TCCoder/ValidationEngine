using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Caching.Tests
{
    public class CacheInvalidationOnWriteAnalyzerTests
    {
        private static CSharpAnalyzerTest<CacheInvalidationOnWriteAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            return CacheManagerTestSupport.Create<CacheInvalidationOnWriteAnalyzer>(source);
        }

        [Fact]
        public async Task WhenWriteMethodDoesNotInvalidateCacheThenCache010IsReported()
        {
            const string source = @"
using System.Threading;
using System.Threading.Tasks;
using RedisCacheManager.Caching;

namespace TestApp
{
    public class ClientConfigService
    {
        private readonly ICacheManager _cache;

        public ClientConfigService(ICacheManager cache)
        {
            _cache = cache;
        }

        public async Task {|#0:UpdateClientConfigAsync|}(int tenantId, int clientId, object config, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(CacheInvalidationOnWriteAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("UpdateClientConfigAsync"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenWriteMethodInvalidatesCacheWithoutCancellationTokenThenCache010IsReported()
        {
            const string source = @"
using System.Threading;
using System.Threading.Tasks;
using RedisCacheManager.Caching;

namespace TestApp
{
    public class ClientConfigService
    {
        private readonly ICacheManager _cache;

        public ClientConfigService(ICacheManager cache)
        {
            _cache = cache;
        }

        public async Task {|#0:UpdateClientConfigAsync|}(int tenantId, int clientId, object config, CancellationToken cancellationToken)
        {
            await _cache.RemoveAsync(""ClientConfig"", ""key"");
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(CacheInvalidationOnWriteAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("UpdateClientConfigAsync"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenWriteMethodInvalidatesCacheWithCancellationTokenThenNoDiagnosticIsReported()
        {
            const string source = @"
using System.Threading;
using System.Threading.Tasks;
using RedisCacheManager.Caching;

namespace TestApp
{
    public class ClientConfigService
    {
        private readonly ICacheManager _cache;

        public ClientConfigService(ICacheManager cache)
        {
            _cache = cache;
        }

        public async Task UpdateClientConfigAsync(int tenantId, int clientId, object config, CancellationToken cancellationToken)
        {
            await _cache.RemoveAsync(""ClientConfig"", ""key"", cancellationToken);
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenNonCacheAwareServiceHasWriteMethodThenNoDiagnosticIsReported()
        {
            const string source = @"
using System.Threading;
using System.Threading.Tasks;

namespace TestApp
{
    public class ClientConfigRepository
    {
        public async Task UpdateClientConfigAsync(int tenantId, int clientId, object config, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenReadMethodInCacheAwareServiceDoesNotInvalidateCacheThenNoDiagnosticIsReported()
        {
            const string source = @"
using System.Threading;
using System.Threading.Tasks;
using RedisCacheManager.Caching;

namespace TestApp
{
    public class ClientConfigService
    {
        private readonly ICacheManager _cache;

        public ClientConfigService(ICacheManager cache)
        {
            _cache = cache;
        }

        public async Task<string?> GetClientConfigAsync(int tenantId, int clientId, CancellationToken cancellationToken)
        {
            var (_, value) = await _cache.TryGetAsync<string>(""ClientConfig"", ""key"", cancellationToken);
            return value;
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
