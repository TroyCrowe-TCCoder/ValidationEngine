using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Performance
{
	/// <summary>
	/// Enforces GlobalPerformanceStandards.md performance.3.8: HTTP client timeouts must be
	/// configured via the 'timeout' parameter on 'IRequestProcessor' methods, not by setting
	/// 'HttpClient.Timeout' directly. Setting 'HttpClient.Timeout' creates a competing timeout
	/// mechanism alongside 'IRequestProcessor'&apos;s own timeout composition. Flags any assignment
	/// to a '.Timeout' member on an expression whose static type is 'HttpClient'.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public sealed class HttpClientTimeoutSetDirectlyAnalyzer : DiagnosticAnalyzer
	{
		public const string DiagnosticId = "PERF002";

		private static readonly LocalizableString Title =
			"HttpClient.Timeout must not be set directly";

		private static readonly LocalizableString MessageFormat =
			"'HttpClient.Timeout' must not be set directly; supply the timeout via the 'timeout' parameter on IRequestProcessor methods instead, per GlobalPerformanceStandards.md performance.3.8";

		private static readonly LocalizableString Description =
			"HTTP client timeouts must be configured via the timeout parameter on IRequestProcessor " +
			"methods, not by setting HttpClient.Timeout directly. IRequestProcessor owns cancellation " +
			"token forwarding for all outbound HTTP calls and composes the caller-supplied " +
			"CancellationToken with the timeout deadline internally. Setting HttpClient.Timeout " +
			"alongside IRequestProcessor's timeout parameter creates two competing timeout mechanisms " +
			"on the same request with no predictable winner. See GlobalPerformanceStandards.md Section 3.8.";

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
				compilationContext.RegisterSyntaxNodeAction(AnalyzeAssignment, Microsoft.CodeAnalysis.CSharp.SyntaxKind.SimpleAssignmentExpression);
			});
		}

		private static void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
		{
			var assignment = (AssignmentExpressionSyntax)context.Node;

			if (assignment.Left is not MemberAccessExpressionSyntax memberAccess)
			{
				return;
			}

			if (!string.Equals(memberAccess.Name.Identifier.ValueText, PerformanceWellKnownTypes.TimeoutPropertyName, System.StringComparison.Ordinal))
			{
				return;
			}

			TypeInfo targetTypeInfo = context.SemanticModel.GetTypeInfo(memberAccess.Expression, context.CancellationToken);
			ITypeSymbol? targetType = targetTypeInfo.Type;

			if (targetType is null || targetType.ToDisplayString() != PerformanceWellKnownTypes.HttpClientMetadataName)
			{
				return;
			}

			context.ReportDiagnostic(Diagnostic.Create(Rule, assignment.GetLocation()));
		}
	}
}