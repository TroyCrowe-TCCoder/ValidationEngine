using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Performance.Tests
{
	public class AsyncMethodMissingCancellationTokenAnalyzerTests
	{
		private static CSharpAnalyzerTest<AsyncMethodMissingCancellationTokenAnalyzer, DefaultVerifier> CreateTest(string source)
		{
			var test = new CSharpAnalyzerTest<AsyncMethodMissingCancellationTokenAnalyzer, DefaultVerifier>
			{
				ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
				TestCode = source,
			};

			return test;
		}

		[Fact]
		public async Task WhenAsyncMethodMissingCancellationTokenThenDiagnosticReported()
		{
			const string source = @"
using System.Threading.Tasks;

namespace TestApp
{
	public class Service
	{
		public async Task {|#0:DoWorkAsync|}()
		{
			await Task.Delay(1);
		}
	}
}";

			var test = CreateTest(source);
			test.ExpectedDiagnostics.Add(
				new DiagnosticResult(AsyncMethodMissingCancellationTokenAnalyzer.Rule)
					.WithLocation(0)
					.WithArguments("DoWorkAsync"));

			await test.RunAsync();
		}

		[Fact]
		public async Task WhenAsyncMethodHasCancellationTokenThenNoDiagnosticReported()
		{
			const string source = @"
using System.Threading;
using System.Threading.Tasks;

namespace TestApp
{
	public class Service
	{
		public async Task DoWorkAsync(CancellationToken cancellationToken)
		{
			await Task.Delay(1, cancellationToken);
		}
	}
}";

			var test = CreateTest(source);
			await test.RunAsync();
		}
	}
}