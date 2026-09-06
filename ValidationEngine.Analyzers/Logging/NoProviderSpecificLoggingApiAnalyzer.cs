using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Logging
{
    /// <summary>
    /// Enforces GlobalLoggingStandards.md logging.2.2: application code must not call
    /// TelemetryClient.Track* directly; ILogger&lt;T&gt; (Microsoft.Extensions.Logging) is the only
    /// approved logging abstraction, with Application Insights wired in as a destination only.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class NoProviderSpecificLoggingApiAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "LOG002";

        private static readonly LocalizableString Title =
            "TelemetryClient.Track* must not be called directly";

        private static readonly LocalizableString MessageFormat =
            "'{0}' calls TelemetryClient.{1} directly; use ILogger<T> (Microsoft.Extensions.Logging) instead, per GlobalLoggingStandards.md logging.2.2";

        private static readonly LocalizableString Description =
            "ILogger<T> is the only approved logging abstraction for all applications. Direct calls to " +
            "TelemetryClient.Track* bypass the logging abstraction and couple application code to " +
            "Application Insights. See GlobalLoggingStandards.md Section 2.2.";

        private const string Category = "Logging";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
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
                INamedTypeSymbol? telemetryClientSymbol =
                    compilationContext.Compilation.GetTypeByMetadataName(LoggingWellKnownTypes.TelemetryClientMetadataName);

                if (telemetryClientSymbol is null)
                {
                    // Microsoft.ApplicationInsights is not referenced by this compilation; nothing to flag.
                    return;
                }

                compilationContext.RegisterSyntaxNodeAction(
                    syntaxContext => AnalyzeInvocation(syntaxContext, telemetryClientSymbol),
                    SyntaxKind.InvocationExpression);
            });
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context, INamedTypeSymbol telemetryClientSymbol)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            {
                return;
            }

            ISymbol? symbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol;

            if (symbol is not IMethodSymbol methodSymbol || methodSymbol.ContainingType is null)
            {
                return;
            }

            if (!SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, telemetryClientSymbol))
            {
                return;
            }

            if (!LoggingWellKnownTypes.TelemetryClientTrackMethodNames.Contains(methodSymbol.Name, StringComparer.Ordinal))
            {
                return;
            }

            string containingMethodName =
                LoggingWellKnownTypes.GetContainingMethod(invocation)?.Identifier.ValueText ?? "<unknown>";

            var diagnostic = Diagnostic.Create(
                Rule,
                memberAccess.Name.GetLocation(),
                containingMethodName,
                methodSymbol.Name);

            context.ReportDiagnostic(diagnostic);
        }
    }
}
