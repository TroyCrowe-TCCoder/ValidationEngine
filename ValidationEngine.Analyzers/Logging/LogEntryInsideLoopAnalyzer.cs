using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Logging
{
    /// <summary>
    /// Enforces GlobalLoggingStandards.md logging.9: log entries written in a tight loop must be
    /// rate-limited or aggregated rather than emitting one log entry per iteration, which can flood
    /// log sinks, queues, and databases. This analyzer flags <c>ILogger&lt;T&gt;</c> calls found
    /// directly inside a <c>for</c>/<c>foreach</c>/<c>while</c>/<c>do</c> loop body.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LogEntryInsideLoopAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "LOG016";

        private static readonly LocalizableString Title =
            "Log entries inside loops must be rate-limited or aggregated";

        private static readonly LocalizableString MessageFormat =
            "'{0}' calls the logger once per loop iteration; emit a rate-limited or aggregated summary entry instead of logging per item, per GlobalLoggingStandards.md logging.9";

        private static readonly LocalizableString Description =
            "Log entries written in a tight loop (for example, processing a large import batch) must be " +
            "rate-limited or aggregated. A summary entry must be emitted rather than one entry per item when " +
            "batch sizes exceed 100 items. See GlobalLoggingStandards.md Section 9.";

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
                    syntaxContext => AnalyzeInvocation(syntaxContext, loggerGenericSymbol, loggerSymbol),
                    SyntaxKind.InvocationExpression);
            });
        }

        private static void AnalyzeInvocation(
            SyntaxNodeAnalysisContext context,
            INamedTypeSymbol? loggerGenericSymbol,
            INamedTypeSymbol? loggerSymbol)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            {
                return;
            }

            string methodName = memberAccess.Name.Identifier.ValueText;

            if (!LoggingWellKnownTypes.IsSeverityLogMethodName(methodName))
            {
                // Only rate-limit the severity-level log calls (LogTrace..LogCritical); BeginScope and
                // the raw Log(...) overload are not the per-item pattern this rule targets.
                return;
            }

            if (LoggingWellKnownTypes.FindEnclosingLoop(invocation) is null)
            {
                return;
            }

            ISymbol? symbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol;

            if (symbol is not IMethodSymbol methodSymbol || methodSymbol.ReceiverType is null)
            {
                return;
            }

            if (!LoggingWellKnownTypes.IsLoggerReceiverType(methodSymbol.ReceiverType, loggerGenericSymbol, loggerSymbol))
            {
                return;
            }

            string containingMethodName =
                LoggingWellKnownTypes.GetContainingMethod(invocation)?.Identifier.ValueText ?? "<unknown>";

            var diagnostic = Diagnostic.Create(Rule, memberAccess.Name.GetLocation(), containingMethodName);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
