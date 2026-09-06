using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Caching.Tests
{
    public class CacheLongLivedExpirationAnalyzerTests
    {
        private static CSharpAnalyzerTest<CacheLongLivedExpirationAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            var test = new CSharpAnalyzerTest<CacheLongLivedExpirationAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.AdditionalReferences.Add(typeof(Microsoft.Extensions.Caching.Distributed.IDistributedCache).Assembly);
            test.TestState.Sources.Add(@"
namespace TestApp
{
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public sealed class LongLivedCacheAttribute : System.Attribute
    {
    }
}
");

            return test;
        }

        [Fact]
        public async Task WhenLongLivedCacheMethodSetsAbsoluteExpirationThenCache016IsReported()
        {
            const string source = @"
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace TestApp
{
    public class ItemTypeService
    {
        private readonly IDistributedCache _cache;

        public ItemTypeService(IDistributedCache cache)
        {
            _cache = cache;
        }

        [LongLivedCache]
        public async Task SetItemTypesAsync(string key, byte[] value)
        {
            var options = new DistributedCacheEntryOptions();
            {|#0:options.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)|};

            await _cache.SetAsync(key, value, options);
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(CacheLongLivedExpirationAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("SetItemTypesAsync", "AbsoluteExpirationRelativeToNow"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenLongLivedCacheMethodCallsSetAbsoluteExpirationThenCache016IsReported()
        {
            const string source = @"
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace TestApp
{
    public class ItemTypeService
    {
        private readonly IDistributedCache _cache;

        public ItemTypeService(IDistributedCache cache)
        {
            _cache = cache;
        }

        [LongLivedCache]
        public async Task SetItemTypesAsync(string key, byte[] value)
        {
            var options = new DistributedCacheEntryOptions();
            {|#0:options.SetAbsoluteExpiration(TimeSpan.FromHours(1))|};

            await _cache.SetAsync(key, value, options);
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(CacheLongLivedExpirationAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("SetItemTypesAsync", "SetAbsoluteExpiration"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenLongLivedCacheMethodSetsNoExpirationThenNoDiagnosticIsReported()
        {
            const string source = @"
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace TestApp
{
    public class ItemTypeService
    {
        private readonly IDistributedCache _cache;

        public ItemTypeService(IDistributedCache cache)
        {
            _cache = cache;
        }

        [LongLivedCache]
        public async Task SetItemTypesAsync(string key, byte[] value)
        {
            await _cache.SetAsync(key, value);
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenMethodIsNotMarkedLongLivedCacheThenNoDiagnosticIsReported()
        {
            const string source = @"
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace TestApp
{
    public class UserProfileService
    {
        private readonly IDistributedCache _cache;

        public UserProfileService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task SetUserProfileAsync(string key, byte[] value)
        {
            var options = new DistributedCacheEntryOptions();
            options.SetAbsoluteExpiration(TimeSpan.FromMinutes(30));

            await _cache.SetAsync(key, value, options);
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
