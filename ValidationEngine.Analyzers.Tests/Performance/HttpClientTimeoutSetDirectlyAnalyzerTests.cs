using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Performance.Tests
{
	public class HttpClientTimeoutSetDirectlyAnalyzerTests
	{
		private static CSharpAnalyzerTest<HttpClientTimeoutSetDirectlyAnalyzer, DefaultVerifier> CreateTest(string source)
		{
			var test = new CSharpAnalyzerTest<HttpClientTimeoutSetDirectlyAnalyzer, DefaultVerifier>
			{
				ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
				TestState = { Sources = { source } },
			};

			return test;
		}

		[Fact]
		public async Task WhenHttpClientTimeoutSetDirectlyThenDiagnosticReported()
		{
			const string source = @"
using System;
using System.Net.Http;

namespace TestApp
{
	public class Service
	{
		public void Configure(HttpClient httpClient)
		{
			{|#0:httpClient.Timeout = TimeSpan.FromSeconds(30)|};
		}
	}
}";

			var test = CreateTest(source);
			test.ExpectedDiagnostics.Add(
				new DiagnosticResult(HttpClientTimeoutSetDirectlyAnalyzer.Rule)
					.WithLocation(0));

			await test.RunAsync();
		}

		[Fact]
		public async Task WhenTimeoutSetOnUnrelatedTypeThenNoDiagnosticReported()
		{
			const string source = @"
using System;

namespace TestApp
{
	public class Options
	{
		public TimeSpan Timeout { get; set; }
	}

	public class Service
	{
		public void Configure(Options options)
		{
			options.Timeout = TimeSpan.FromSeconds(30);
		}
	}
}";

			var test = CreateTest(source);
			await test.RunAsync();
		}
	}
}