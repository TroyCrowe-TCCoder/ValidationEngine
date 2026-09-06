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
            var test = new CSharpAnalyzerTest<NoSensitiveDataInCacheAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.AdditionalReferences.Add(typeof(Microsoft.Extensions.Caching.Distributed.IDistributedCache).Assembly);

            return test;
        }

        [Fact]
        public async Task WhenCachedValueIdentifierNameIsSensitiveThenCache011IsReported()
        {
            const string source = @"
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace TestApp
{
    public class SessionService
    {
        private readonly IDistributedCache _cache;

        public SessionService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public Task CacheAuthTokenAsync(string key, byte[] authToken)
            => _cache.SetAsync(key, {|#0:authToken|});
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
using Microsoft.Extensions.Caching.Distributed;

namespace TestApp
{
    public class Account
    {
        public byte[] CardNumber { get; set; } = System.Array.Empty<byte>();
    }

    public class BillingService
    {
        private readonly IDistributedCache _cache;

        public BillingService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public Task CacheCardAsync(string key, Account account)
            => _cache.SetAsync(key, {|#0:account.CardNumber|});
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
using Microsoft.Extensions.Caching.Distributed;

namespace TestApp
{
    public class UserRecord
    {
        public string UserId { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class UserService
    {
        private readonly IDistributedCache _cache;

        public UserService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public Task CacheUserAsync(string key, UserRecord user)
            => _cache.SetAsync(key, {|#0:CacheSerializer.Serialize(user)|});
    }

    public static class CacheSerializer
    {
        public static byte[] Serialize<T>(T value)
            => System.Text.Encoding.UTF8.GetBytes(value?.ToString() ?? string.Empty);
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
using Microsoft.Extensions.Caching.Distributed;

namespace TestApp
{
    public class ProductSummary
    {
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public class CatalogService
    {
        private readonly IDistributedCache _cache;

        public CatalogService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public Task CacheProductAsync(string key, ProductSummary product)
            => _cache.SetAsync(key, CacheSerializer.Serialize(product));
    }

    public static class CacheSerializer
    {
        public static byte[] Serialize<T>(T value)
            => System.Text.Encoding.UTF8.GetBytes(value?.ToString() ?? string.Empty);
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
