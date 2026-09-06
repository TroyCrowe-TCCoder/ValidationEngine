using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Caching.Tests
{
    public class NoWildcardKeyDeletionAnalyzerTests
    {
        private static CSharpAnalyzerTest<NoWildcardKeyDeletionAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            var test = new CSharpAnalyzerTest<NoWildcardKeyDeletionAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.AdditionalReferences.Add(typeof(StackExchange.Redis.IServer).Assembly);
            test.TestState.AdditionalReferences.Add(typeof(Microsoft.Extensions.Caching.Distributed.IDistributedCache).Assembly);

            return test;
        }

        [Fact]
        public async Task WhenServerKeysIsCalledWithWildcardThenCache009IsReported()
        {
            const string source = @"
using StackExchange.Redis;

namespace TestApp
{
    public class CacheInvalidationService
    {
        public void InvalidateGroup(IServer server, int tenantId)
        {
            var keys = {|#0:server.Keys(pattern: $""tenant:{tenantId}:itemtypes:*"")|};
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(NoWildcardKeyDeletionAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("IServer.Keys(...)"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenDatabaseExecuteAsyncIssuesRawKeysCommandThenCache009IsReported()
        {
            const string source = @"
using System.Threading.Tasks;
using StackExchange.Redis;

namespace TestApp
{
    public class CacheInvalidationService
    {
        public async Task InvalidateGroupAsync(IDatabase database, int tenantId)
        {
            await {|#0:database.ExecuteAsync(""KEYS"", $""tenant:{tenantId}:*"")|};
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(NoWildcardKeyDeletionAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("ExecuteAsync(\"KEYS\", ...)"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenExplicitCacheKeysAreRemovedThenNoDiagnosticIsReported()
        {
            const string source = @"
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace TestApp
{
    public static class CacheKeys
    {
        public static string ItemTypes(int tenantId) => $""tenant:{tenantId}:itemtypes"";

        public static string ItemTypeDescriptions(int tenantId, int clientId, int itemTypeId) =>
            $""tenant:{tenantId}:client:{clientId}:itemtypes:{itemTypeId}:descriptions"";
    }

    public class CacheInvalidationService
    {
        private readonly IDistributedCache _cache;

        public CacheInvalidationService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task InvalidateGroupAsync(int tenantId, int clientId, int itemTypeId, CancellationToken cancellationToken)
        {
            await _cache.RemoveAsync(CacheKeys.ItemTypes(tenantId), cancellationToken);
            await _cache.RemoveAsync(CacheKeys.ItemTypeDescriptions(tenantId, clientId, itemTypeId), cancellationToken);
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenDatabaseExecutesUnrelatedCommandThenNoDiagnosticIsReported()
        {
            const string source = @"
using StackExchange.Redis;

namespace TestApp
{
    public class RedisJsonHelper
    {
        public RedisResult Get(IDatabase database, string key)
            => database.Execute(""JSON.GET"", key, ""$"");
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
