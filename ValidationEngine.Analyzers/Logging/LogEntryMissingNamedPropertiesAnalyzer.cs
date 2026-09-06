using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Logging
{
    /// <summary>
    /// Enforces GlobalLoggingStandards.md logging.3.3: every Information-or-higher log entry must
    /// include named properties (operation name, entity identifiers, correlation identifier) rather
    /// than a free-text message with no structured data.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LogEntryMissingNamedPropertiesAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "LOG007";

        private static readonly LocalizableString Title =
            "Information-or-higher log entries must include named properties";

        private static readonly LocalizableString MessageFormat =
            "'{0}' calls {1} with a free-text message and no named placeholders; include entity identifiers and the correlation identifier as named properties, per GlobalLoggingStandards.md logging.3.3";

        private static readonly LocalizableString Description =
            "Every Information log entry and every higher-severity log entry must include the operation name and " +
            "relevant entity identifiers as named properties, plus the correlation identifier. Free-text " +
            "descriptions must not be used in place of named identifiers. See GlobalLoggingStandards.md Section 3.3.";

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
            int severityRank = LoggingWellKnownTypes.GetSeverityRank(methodName);

            // Information (2) and above must include named properties; Trace/Debug are exempt.
            if (severityRank < 2)
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

            ExpressionSyntax? templateArgument = LoggingWellKnownTypes.GetMessageTemplateArgument(invocation);

            if (templateArgument is not LiteralExpressionSyntax literal)
            {
                // Interpolated strings are handled by LOG005; non-literal templates cannot be
                // statically analyzed for placeholder count.
                return;
            }

            string messageTemplate = literal.Token.ValueText;

            if (LoggingWellKnownTypes.CountNamedPlaceholders(messageTemplate) > 0)
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
