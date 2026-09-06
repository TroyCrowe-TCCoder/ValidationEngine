using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace ValidationEngine.Analyzers.Security
{
	/// <summary>
	/// Enforces GlobalSecurityStandards.md security.2.2: secrets must be stored per environment
	/// (Key Vault for prod/staging, ADO variable groups for CI/CD, .NET User Secrets for local
	/// dev) and must never appear as literal values in 'appsettings*.json' files, especially
	/// 'appsettings.Development.json'. Flags secret-shaped JSON keys (connection string, secret,
	/// password, API key) bound to a non-empty literal value in any 'appsettings*.json' file.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public sealed class SecretInAppSettingsAnalyzer : DiagnosticAnalyzer
	{
		public const string DiagnosticId = "SEC012";

		private const string AppSettingsFileNameFragment = "appsettings";
		private const string JsonFileExtension = ".json";

		private static readonly Regex JsonStringPropertyPattern = new Regex(
			"\"(?<key>[^\"]+)\"\\s*:\\s*\"(?<value>[^\"]*)\"",
			RegexOptions.Compiled);

		private static readonly LocalizableString Title =
			"Secret-shaped values must not be stored in appsettings*.json";

		private static readonly LocalizableString MessageFormat =
			"'{0}' in '{1}' appears to hold a secret value; secrets must be stored in Key Vault, ADO variable groups, or .NET User Secrets, never in appsettings*.json, per GlobalSecurityStandards.md security.2.2";

		private static readonly LocalizableString Description =
			"Secrets must be stored per environment: Azure Key Vault for production/staging, ADO " +
			"variable groups linked to Key Vault for CI/CD, and .NET User Secrets for local " +
			"development. Secret-shaped values (connection strings, API keys, JWT signing keys, " +
			"storage keys, passwords) must never be committed as literal values in any " +
			"appsettings*.json file, and especially not appsettings.Development.json. See " +
			"GlobalSecurityStandards.md Section 2.2.";

		private const string Category = "Security";

		public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			DiagnosticId,
			Title,
			MessageFormat,
			Category,
			DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: Description,
			helpLinkUri: "https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalSecurityStandards.md",
			customTags: new[] { "CompilationEnd" });

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
			ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();

			context.RegisterCompilationAction(AnalyzeAdditionalFiles);
		}

		private static void AnalyzeAdditionalFiles(CompilationAnalysisContext context)
		{
			foreach (AdditionalText additionalFile in context.Options.AdditionalFiles)
			{
				string filePath = additionalFile.Path;
				string fileName = System.IO.Path.GetFileName(filePath);

				if (!fileName.EndsWith(JsonFileExtension, System.StringComparison.OrdinalIgnoreCase) ||
					fileName.IndexOf(AppSettingsFileNameFragment, System.StringComparison.OrdinalIgnoreCase) < 0)
				{
					continue;
				}

				SourceText? text = additionalFile.GetText(context.CancellationToken);
				if (text is null)
				{
					continue;
				}

				foreach (TextLine line in text.Lines)
				{
					string lineText = line.ToString();

					foreach (Match match in JsonStringPropertyPattern.Matches(lineText))
					{
						string key = match.Groups["key"].Value;
						string value = match.Groups["value"].Value;

						if (string.IsNullOrEmpty(value) || !SecurityWellKnownTypes.IsSecretShapedName(key))
						{
							continue;
						}

						var lineSpan = new LinePositionSpan(
							new LinePosition(line.LineNumber, 0),
							new LinePosition(line.LineNumber, lineText.Length));
						var location = Location.Create(filePath, line.Span, lineSpan);

						context.ReportDiagnostic(Diagnostic.Create(Rule, location, key, fileName));
					}
				}
			}
		}
	}
}