using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Logging
{
    /// <summary>
    /// Enforces GlobalLoggingStandards.md logging.3.2: errors must not be downgraded to a lower
    /// severity to suppress alert noise. A caught exception logged through ILogger&lt;T&gt; must be
    /// logged at <c>Error</c> or <c>Critical</c> severity, not <c>Trace</c>/<c>Debug</c>/
    /// <c>Information</c>/<c>Warning</c>.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ExceptionLogSeverityAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "LOG006";

        private static readonly LocalizableString Title =
            "Caught exceptions must be logged at Error or Critical severity";

        private static readonly LocalizableString MessageFormat =
            "'{0}' logs caught exception '{1}' using {2}, which downgrades an error below Error severity; use LogError or LogCritical instead, per GlobalLoggingStandards.md logging.3.2";

        private static readonly LocalizableString Description =
            "Log levels must reflect the true severity of the underlying condition. A failure captured by a catch " +
            "block must not be downgraded to Trace, Debug, Information, or Warning to suppress alert noise. See " +
            "GlobalLoggingStandards.md Section 3.2.";

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
                INamedTypeSymbol? loggerGenericSymbol =
                    compilationContext.Compilation.GetTypeByMetadataName(LoggingWellKnownTypes.LoggerInterfaceMetadataName);
                INamedTypeSymbol? loggerSymbol =
                    compilationContext.Compilation.GetTypeByMetadataName(LoggingWellKnownTypes.LoggerNonGenericInterfaceMetadataName);

                if (loggerGenericSymbol is null && loggerSymbol is null)
                {
                    // Microsoft.Extensions.Logging.Abstractions is not referenced by this compilation.
                    return;
                }

                compilationContext.RegisterSyntaxNodeAction(
                    syntaxContext => AnalyzeCatchClause(syntaxContext, loggerGenericSymbol, loggerSymbol),
                    SyntaxKind.CatchClause);
            });
        }

        private static void AnalyzeCatchClause(
            SyntaxNodeAnalysisContext context,
            INamedTypeSymbol? loggerGenericSymbol,
            INamedTypeSymbol? loggerSymbol)
        {
            var catchClause = (CatchClauseSyntax)context.Node;

            string? exceptionIdentifier = catchClause.Declaration?.Identifier.ValueText;

            if (string.IsNullOrEmpty(exceptionIdentifier) || catchClause.Block is null)
            {
                return;
            }

            foreach (InvocationExpressionSyntax invocation in catchClause.Block.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                {
                    continue;
                }

                string methodName = memberAccess.Name.Identifier.ValueText;
                int severityRank = LoggingWellKnownTypes.GetSeverityRank(methodName);

                // Rank 4 = LogError, 5 = LogCritical; anything lower (including -1 for BeginScope/Log) is a downgrade
                // only when it is one of the recognized severity methods below Error.
                if (severityRank < 0 || severityRank >= 4)
                {
                    continue;
                }

                bool passesExceptionArgument = invocation.ArgumentList.Arguments.Any(argument =>
                    argument.Expression is IdentifierNameSyntax identifier &&
                    string.Equals(identifier.Identifier.ValueText, exceptionIdentifier, System.StringComparison.Ordinal));

                if (!passesExceptionArgument)
                {
                    continue;
                }

                ISymbol? symbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol;

                if (symbol is not IMethodSymbol methodSymbol || methodSymbol.ReceiverType is null)
                {
                    continue;
                }

                if (!LoggingWellKnownTypes.IsLoggerReceiverType(methodSymbol.ReceiverType, loggerGenericSymbol, loggerSymbol))
                {
                    continue;
                }

                string containingMethodName =
                    LoggingWellKnownTypes.GetContainingMethod(invocation)?.Identifier.ValueText ?? "<unknown>";

                var diagnostic = Diagnostic.Create(
                    Rule,
                    memberAccess.Name.GetLocation(),
                    containingMethodName,
                    exceptionIdentifier,
                    methodName);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
