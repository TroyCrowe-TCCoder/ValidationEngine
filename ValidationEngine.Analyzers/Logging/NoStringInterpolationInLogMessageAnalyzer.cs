using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Logging
{
    /// <summary>
    /// Enforces GlobalLoggingStandards.md logging.2.6 / logging.3.1: all ILogger&lt;T&gt; log
    /// entries must use named structured properties; string interpolation must not be used in a
    /// log message template.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class NoStringInterpolationInLogMessageAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "LOG005";

        private static readonly LocalizableString Title =
            "Log message templates must not use string interpolation";

        private static readonly LocalizableString MessageFormat =
            "'{0}' uses string interpolation in the log message template; use named placeholders such as {{PropertyName}} instead, per GlobalLoggingStandards.md logging.2.6/3.1";

        private static readonly LocalizableString Description =
            "All log entries must use named structured properties rather than string interpolation in the message " +
            "template, so the properties remain queryable in the logging backend. See GlobalLoggingStandards.md Section 2.6/3.1.";

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

            if (!LoggingWellKnownTypes.IsLoggerMethodName(memberAccess.Name.Identifier.ValueText))
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

            if (templateArgument is not InterpolatedStringExpressionSyntax)
            {
                return;
            }

            string containingMethodName =
                LoggingWellKnownTypes.GetContainingMethod(invocation)?.Identifier.ValueText ?? "<unknown>";

            var diagnostic = Diagnostic.Create(Rule, templateArgument.GetLocation(), containingMethodName);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
