using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Security.Tests
{
	public class SecretInAppSettingsAnalyzerTests
	{
		private static CSharpAnalyzerTest<SecretInAppSettingsAnalyzer, DefaultVerifier> CreateTest()
		{
			var test = new CSharpAnalyzerTest<SecretInAppSettingsAnalyzer, DefaultVerifier>
			{
				ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
			};

			test.TestState.Sources.Add(("Program.cs", "namespace TestApp { public class Program { public static void Main() { } } }"));

			return test;
		}

		[Fact]
		public async Task WhenAppSettingsContainsSecretShapedValueThenDiagnosticReported()
		{
			var test = CreateTest();

			string json = "{\n  \"DbPassword\": \"abc123\"\n}\n";

			test.TestState.AdditionalFiles.Add(("appsettings.Development.json", json));
			test.ExpectedDiagnostics.Add(
				new DiagnosticResult(SecretInAppSettingsAnalyzer.Rule)
					.WithSpan("appsettings.Development.json", 2, 1, 2, 25)
					.WithArguments("DbPassword", "appsettings.Development.json"));

			await test.RunAsync();
		}

		[Fact]
		public async Task WhenAppSettingsHasNoSecretShapedKeyThenNoDiagnosticReported()
		{
			var test = CreateTest();

			string json = "{\n  \"Logging\": {\n    \"LogLevel\": \"Information\"\n  }\n}\n";

			test.TestState.AdditionalFiles.Add(("appsettings.json", json));

			await test.RunAsync();
		}
	}
}