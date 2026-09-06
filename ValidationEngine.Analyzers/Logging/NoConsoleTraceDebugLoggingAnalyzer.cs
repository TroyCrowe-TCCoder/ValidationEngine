using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Logging
{
    /// <summary>
    /// Enforces GlobalLoggingStandards.md logging.2.1: Console.Write*, Trace.Write*, and Debug.Write*
    /// must not be used as logging mechanisms; ILogger&lt;T&gt; (Microsoft.Extensions.Logging) is the
    /// only approved logging abstraction.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class NoConsoleTraceDebugLoggingAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "LOG001";

        private static readonly LocalizableString Title =
            "Console, Trace, and Debug must not be used for logging";

        private static readonly LocalizableString MessageFormat =
            "'{0}.{1}' is used for logging; use ILogger<T> (Microsoft.Extensions.Logging) instead, per GlobalLoggingStandards.md logging.2.1";

        private static readonly LocalizableString Description =
            "ILogger<T> is the only approved logging abstraction for all applications. Console, Trace, " +
            "and Debug write APIs must not be used as logging mechanisms. See GlobalLoggingStandards.md Section 2.1.";

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
                INamedTypeSymbol? consoleSymbol =
                    compilationContext.Compilation.GetTypeByMetadataName(LoggingWellKnownTypes.ConsoleMetadataName);
                INamedTypeSymbol? traceSymbol =
                    compilationContext.Compilation.GetTypeByMetadataName(LoggingWellKnownTypes.TraceMetadataName);
                INamedTypeSymbol? debugSymbol =
                    compilationContext.Compilation.GetTypeByMetadataName(LoggingWellKnownTypes.DebugMetadataName);

                if (consoleSymbol is null && traceSymbol is null && debugSymbol is null)
                {
                    return;
                }

                compilationContext.RegisterSyntaxNodeAction(
                    syntaxContext => AnalyzeInvocation(syntaxContext, consoleSymbol, traceSymbol, debugSymbol),
                    SyntaxKind.InvocationExpression);
            });
        }

        private static void AnalyzeInvocation(
            SyntaxNodeAnalysisContext context,
            INamedTypeSymbol? consoleSymbol,
            INamedTypeSymbol? traceSymbol,
            INamedTypeSymbol? debugSymbol)
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

            INamedTypeSymbol containingType = methodSymbol.ContainingType;

            bool isBannedType =
                SymbolEqualityComparer.Default.Equals(containingType, consoleSymbol) ||
                SymbolEqualityComparer.Default.Equals(containingType, traceSymbol) ||
                SymbolEqualityComparer.Default.Equals(containingType, debugSymbol);

            if (!isBannedType)
            {
                return;
            }

            var diagnostic = Diagnostic.Create(
                Rule,
                memberAccess.GetLocation(),
                containingType.Name,
                methodSymbol.Name);

            context.ReportDiagnostic(diagnostic);
        }
    }
}
