using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Security
{
    /// <summary>
    /// Enforces GlobalSecurityStandards.md security.5.4: outbound query string values must be
    /// encoded via 'Uri.EscapeDataString', never 'Uri.EscapeUriString'. 'Uri.EscapeUriString' does
    /// not encode reserved characters used to delimit query parameters (e.g. '&amp;', '='),
    /// making it unsafe for encoding individual query parameter values. Flags any invocation of
    /// 'Uri.EscapeUriString'.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UnsafeUriEscapingAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SEC006";

        private static readonly LocalizableString Title =
            "Uri.EscapeUriString must not be used";

        private static readonly LocalizableString MessageFormat =
            "'Uri.EscapeUriString' is not permitted; use 'Uri.EscapeDataString' to encode query parameter values, per GlobalSecurityStandards.md security.5.4";

        private static readonly LocalizableString Description =
            "Uri.EscapeUriString must not be used anywhere in the codebase. It does not encode " +
            "reserved characters used to delimit query parameters, making it unsafe for encoding " +
            "individual query string values. Use Uri.EscapeDataString instead, or the parameterised " +
            "RequestManager overloads that encode internally. See GlobalSecurityStandards.md Section 5.4.";

        private const string Category = "Security";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: "https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalSecurityStandards.md");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(compilationContext =>
            {
                compilationContext.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
            });
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            {
                return;
            }

            if (!string.Equals(memberAccess.Name.Identifier.ValueText, SecurityWellKnownTypes.EscapeUriStringMethodName, System.StringComparison.Ordinal))
            {
                return;
            }

            if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol methodSymbol ||
                methodSymbol.ContainingType?.ToDisplayString() != SecurityWellKnownTypes.UriMetadataName)
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
        }
    }
}
