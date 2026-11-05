using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Caching.Tests
{
    public class NoSensitiveDataInCacheAnalyzerTests
    {
        private static CSharpAnalyzerTest<NoSensitiveDataInCacheAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            return CacheManagerTestSupport.Create<NoSensitiveDataInCacheAnalyzer>(source);
        }

        [Fact]
        public async Task WhenCachedValueIdentifierNameIsSensitiveThenCache011IsReported()
        {
            const string source = @"
using System.Threading.Tasks;
using RedisCacheManager.Caching;

namespace TestApp
{
    public class SessionService
    {
        private readonly ICacheManager _cache;

        public SessionService(ICacheManager cache)
        {
            _cache = cache;
        }

        public Task CacheAuthTokenAsync(string category, string id, string authToken)
            => _cache.SetAsync(category, id, {|#0:authToken|});
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(NoSensitiveDataInCacheAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("authToken"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenCachedValueMemberAccessIsSensitiveThenCache011IsReported()
        {
            const string source = @"
using System.Threading.Tasks;
using RedisCacheManager.Caching;

namespace TestApp
{
    public class Account
    {
        public string CardNumber { get; set; } = string.Empty;
    }

    public class BillingService
    {
        private readonly ICacheManager _cache;

        public BillingService(ICacheManager cache)
        {
            _cache = cache;
        }

        public Task CacheCardAsync(string category, string id, Account account)
            => _cache.SetAsync(category, id, {|#0:account.CardNumber|});
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(NoSensitiveDataInCacheAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("CardNumber"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenCachedValueTypeHasSensitivePropertyThenCache011IsReported()
        {
            const string source = @"
using System.Threading.Tasks;
using RedisCacheManager.Caching;

namespace TestApp
{
    public class UserRecord
    {
        public string UserId { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class UserService
    {
        private readonly ICacheManager _cache;

        public UserService(ICacheManager cache)
        {
            _cache = cache;
        }

        public Task CacheUserAsync(string category, string id, UserRecord user)
            => _cache.SetAsync(category, id, {|#0:user|});
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(NoSensitiveDataInCacheAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("Password"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenCachedValueHasNoSensitiveNamesThenNoDiagnosticIsReported()
        {
            const string source = @"
using System.Threading.Tasks;
using RedisCacheManager.Caching;

namespace TestApp
{
    public class ProductSummary
    {
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public class CatalogService
    {
        private readonly ICacheManager _cache;

        public CatalogService(ICacheManager cache)
        {
            _cache = cache;
        }

        public Task CacheProductAsync(string category, string id, ProductSummary product)
            => _cache.SetAsync(category, id, product);
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
