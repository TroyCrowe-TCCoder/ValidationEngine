using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Logging
{
    /// <summary>
    /// Enforces GlobalLoggingStandards.md logging.4.3: application-to-application HTTP calls must
    /// go through the HttpClientManager library so that correlation-identifier forwarding is
    /// handled consistently, rather than calling HttpClient methods directly.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DirectHttpClientCallBypassesManagerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "LOG010";

        private static readonly LocalizableString Title =
            "Outbound HTTP calls must use HttpClientManager";

        private static readonly LocalizableString MessageFormat =
            "'{0}' calls HttpClient.{1} directly; use HttpClientManager so the correlation identifier is forwarded consistently, per GlobalLoggingStandards.md logging.4.3";

        private static readonly LocalizableString Description =
            "Application-to-application HTTP calls must use the HttpClientManager library so that correlation " +
            "identifier forwarding on the X-Correlation-Id header is handled consistently. See GlobalLoggingStandards.md Section 4.3.";

        private const string Category = "Logging";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: "https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalLoggingStandards.md");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(compilationContext =>
            {
                INamedTypeSymbol? httpClientSymbol =
                    compilationContext.Compilation.GetTypeByMetadataName(LoggingWellKnownTypes.HttpClientMetadataName);

                if (httpClientSymbol is null)
                {
                    // System.Net.Http is not referenced by this compilation.
                    return;
                }

                compilationContext.RegisterSyntaxNodeAction(
                    syntaxContext => AnalyzeInvocation(syntaxContext, httpClientSymbol),
                    SyntaxKind.InvocationExpression);
            });
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context, INamedTypeSymbol httpClientSymbol)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            {
                return;
            }

            string methodName = memberAccess.Name.Identifier.ValueText;

            if (!LoggingWellKnownTypes.DependencyCallMethodNames.Contains(methodName, System.StringComparer.Ordinal))
            {
                return;
            }

            ISymbol? symbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol;

            if (symbol is not IMethodSymbol methodSymbol || methodSymbol.ContainingType is null)
            {
                return;
            }

            if (!SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, httpClientSymbol))
            {
                return;
            }

            TypeDeclarationSyntax? containingType = LoggingWellKnownTypes.GetContainingType(invocation);
            string containingTypeName = containingType?.Identifier.ValueText ?? string.Empty;

            if (LoggingWellKnownTypes.LooksLikeHttpClientManager(containingTypeName))
            {
                return;
            }

            string containingMethodName =
                LoggingWellKnownTypes.GetContainingMethod(invocation)?.Identifier.ValueText ?? "<unknown>";

            var diagnostic = Diagnostic.Create(Rule, memberAccess.Name.GetLocation(), containingMethodName, methodName);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
