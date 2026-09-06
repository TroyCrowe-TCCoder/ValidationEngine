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
            var test = new CSharpAnalyzerTest<CacheInvalidationOnWriteAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.AdditionalReferences.Add(typeof(Microsoft.Extensions.Caching.Distributed.IDistributedCache).Assembly);

            return test;
        }

        [Fact]
        public async Task WhenWriteMethodDoesNotInvalidateCacheThenCache010IsReported()
        {
            const string source = @"
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace TestApp
{
    public class ClientConfigService
    {
        private readonly IDistributedCache _cache;

        public ClientConfigService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task {|#0:UpdateClientConfigAsync|}(int tenantId, int clientId, object config, CancellationToken cancellationToken)
        {
            await System.Threading.Tasks.Task.CompletedTask;
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
using Microsoft.Extensions.Caching.Distributed;

namespace TestApp
{
    public class ClientConfigService
    {
        private readonly IDistributedCache _cache;

        public ClientConfigService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task {|#0:UpdateClientConfigAsync|}(int tenantId, int clientId, object config, CancellationToken cancellationToken)
        {
            await _cache.RemoveAsync(""client-config-key"");
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
using Microsoft.Extensions.Caching.Distributed;

namespace TestApp
{
    public class ClientConfigService
    {
        private readonly IDistributedCache _cache;

        public ClientConfigService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task UpdateClientConfigAsync(int tenantId, int clientId, object config, CancellationToken cancellationToken)
        {
            await _cache.RemoveAsync(""client-config-key"", cancellationToken);
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
            await System.Threading.Tasks.Task.CompletedTask;
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
using Microsoft.Extensions.Caching.Distributed;

namespace TestApp
{
    public class ClientConfigService
    {
        private readonly IDistributedCache _cache;

        public ClientConfigService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<byte[]> GetClientConfigAsync(int tenantId, int clientId, CancellationToken cancellationToken)
        {
            return await _cache.GetAsync(""client-config-key"", cancellationToken);
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
