using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace ValidationEngine.Analyzers.Caching.Tests
{
    /// <summary>
    /// Shared helpers for the caching analyzer tests. The ICacheManager contract is declared inline in
    /// every test source (appended to the end) so the tests do not depend on the TCCoder.RedisCacheManager package.
    /// </summary>
    internal static class CacheManagerTestSupport
    {
        public const string CacheManagerStub = @"
#nullable enable
namespace RedisCacheManager.Caching
{
    public interface ICacheManager
    {
        System.Threading.Tasks.Task<(bool Found, T? Value)> TryGetAsync<T>(string category, string id, System.Threading.CancellationToken ct = default);

        System.Threading.Tasks.Task SetAsync<T>(string category, string id, T value, System.TimeSpan? ttl = null, System.Threading.CancellationToken ct = default);

        System.Threading.Tasks.Task RemoveAsync(string category, string id, System.Threading.CancellationToken ct = default);
    }
}
";

        public static CSharpAnalyzerTest<TAnalyzer, DefaultVerifier> Create<TAnalyzer>(string source, bool referenceCachingAbstractions = false)
            where TAnalyzer : DiagnosticAnalyzer, new()
        {
            var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
            {
                TestCode = source + CacheManagerStub,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            if (referenceCachingAbstractions)
            {
                test.TestState.AdditionalReferences.Add(typeof(Microsoft.Extensions.Caching.Distributed.IDistributedCache).Assembly);
            }

            return test;
        }
    }
}
