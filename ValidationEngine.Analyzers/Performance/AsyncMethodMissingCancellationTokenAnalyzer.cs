using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Performance
{
	/// <summary>
	/// Enforces GlobalPerformanceStandards.md performance.3.7: every async method must accept and
	/// forward a 'CancellationToken'. Flags any method returning 'Task', 'Task&lt;T&gt;', 'ValueTask',
	/// or 'ValueTask&lt;T&gt;' that does not declare a 'CancellationToken' parameter. Interface/abstract
	/// method declarations, overrides, and explicit interface implementations are still flagged
	/// because the token must be threaded through the entire call chain.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public sealed class AsyncMethodMissingCancellationTokenAnalyzer : DiagnosticAnalyzer
	{
		public const string DiagnosticId = "PERF001";

		private static readonly LocalizableString Title =
			"Async methods must accept a CancellationToken";

		private static readonly LocalizableString MessageFormat =
			"'{0}' returns a Task-like type but does not declare a CancellationToken parameter; every async method must accept and forward a CancellationToken, per GlobalPerformanceStandards.md performance.3.7";

		private static readonly LocalizableString Description =
			"Every async method must accept and forward a CancellationToken. Operations that ignore " +
			"cancellation hold threads and resources after the caller has abandoned the request. See " +
			"GlobalPerformanceStandards.md Section 3.7.";

		private const string Category = "Performance";

		public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			DiagnosticId,
			Title,
			MessageFormat,
			Category,
			DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: Description,
			helpLinkUri: "https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalPerformanceStandards.md");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
			ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();

			context.RegisterCompilationStartAction(compilationContext =>
			{
				compilationContext.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
			});
		}

		private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
		{
			var methodDeclaration = (MethodDeclarationSyntax)context.Node;

			if (!IsAsyncOrTaskLike(methodDeclaration))
			{
				return;
			}

			if (methodDeclaration.Body is null && methodDeclaration.ExpressionBody is null)
			{
				// Interface/abstract declaration with no body still must expose the token in its
				// signature so implementers thread it through.
			}

			bool hasCancellationToken = methodDeclaration.ParameterList.Parameters
				.Any(parameter => parameter.Type is IdentifierNameSyntax identifierName &&
					string.Equals(identifierName.Identifier.ValueText, PerformanceWellKnownTypes.CancellationTokenTypeName, System.StringComparison.Ordinal));

			if (hasCancellationToken)
			{
				return;
			}

			context.ReportDiagnostic(Diagnostic.Create(
				Rule,
				methodDeclaration.Identifier.GetLocation(),
				methodDeclaration.Identifier.ValueText));
		}

		private static bool IsAsyncOrTaskLike(MethodDeclarationSyntax methodDeclaration)
		{
			if (methodDeclaration.Modifiers.Any(SyntaxKind.AsyncKeyword))
			{
				return true;
			}

			TypeSyntax returnType = methodDeclaration.ReturnType;

			string? typeName = returnType switch
			{
				IdentifierNameSyntax identifierName => identifierName.Identifier.ValueText,
				GenericNameSyntax genericName => genericName.Identifier.ValueText,
				QualifiedNameSyntax qualifiedName => qualifiedName.Right switch
				{
					IdentifierNameSyntax id => id.Identifier.ValueText,
					GenericNameSyntax gen => gen.Identifier.ValueText,
					_ => null,
				},
				_ => null,
			};

			return string.Equals(typeName, PerformanceWellKnownTypes.TaskTypeName, System.StringComparison.Ordinal) ||
				string.Equals(typeName, PerformanceWellKnownTypes.ValueTaskTypeName, System.StringComparison.Ordinal);
		}
	}
}