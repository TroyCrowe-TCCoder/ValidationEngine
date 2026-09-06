using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Caching.Tests
{
    public class CacheDeserializationFailureHandlingAnalyzerTests
    {
        private static CSharpAnalyzerTest<CacheDeserializationFailureHandlingAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            var test = new CSharpAnalyzerTest<CacheDeserializationFailureHandlingAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.AdditionalReferences.Add(typeof(Microsoft.Extensions.Caching.Distributed.IDistributedCache).Assembly);

            return test;
        }

        [Fact]
        public async Task WhenDeserializeCallHasNoSurroundingTryCatchThenCache013IsReported()
        {
            const string source = @"
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace TestApp
{
    public static class CacheSerializer
    {
        public static T? Deserialize<T>(byte[]? data) => default;
    }

    public class UserProfileService
    {
        private readonly IDistributedCache _cache;

        public UserProfileService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<object?> GetUserProfileAsync(string key)
        {
            var cached = await _cache.GetAsync(key);
            return {|#0:CacheSerializer.Deserialize<object>(cached)|};
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(CacheDeserializationFailureHandlingAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("GetUserProfileAsync"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenDeserializeCallIsInTryCatchThatEvictsEntryThenNoDiagnosticIsReported()
        {
            const string source = @"
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace TestApp
{
    public static class CacheSerializer
    {
        public static T? Deserialize<T>(byte[]? data) => default;
    }

    public class UserProfileService
    {
        private readonly IDistributedCache _cache;

        public UserProfileService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<object?> GetUserProfileAsync(string key)
        {
            var cached = await _cache.GetAsync(key);

            try
            {
                return CacheSerializer.Deserialize<object>(cached);
            }
            catch (Exception)
            {
                await _cache.RemoveAsync(key);
                return null;
            }
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenDeserializeCallIsInTryCatchWithoutEvictionThenCache013IsReported()
        {
            const string source = @"
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace TestApp
{
    public static class CacheSerializer
    {
        public static T? Deserialize<T>(byte[]? data) => default;
    }

    public class UserProfileService
    {
        private readonly IDistributedCache _cache;

        public UserProfileService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<object?> GetUserProfileAsync(string key)
        {
            var cached = await _cache.GetAsync(key);

            try
            {
                return {|#0:CacheSerializer.Deserialize<object>(cached)|};
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(CacheDeserializationFailureHandlingAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("GetUserProfileAsync"));

            await test.RunAsync();
        }
    }
}
