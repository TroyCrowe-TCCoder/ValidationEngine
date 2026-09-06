using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Logging
{
    /// <summary>
    /// Enforces GlobalLoggingStandards.md logging.6: telemetry must be emitted for every call to an
    /// external dependency (database, outbound HTTP, storage, queue). As a static heuristic, this
    /// analyzer flags a method that invokes a recognized dependency-call method but never calls
    /// ILogger&lt;T&gt; anywhere in that method, meaning no dependency name/duration/outcome is ever
    /// recorded for that call.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DependencyCallMissingTelemetryAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "LOG013";

        private static readonly LocalizableString Title =
            "External dependency calls must emit telemetry";

        private static readonly LocalizableString MessageFormat =
            "'{0}' calls dependency method '{1}' but never logs a dependency name, duration, or outcome; emit telemetry for this call, per GlobalLoggingStandards.md logging.6";

        private static readonly LocalizableString Description =
            "Telemetry must be emitted for every call to an external dependency (database queries, outbound " +
            "HTTP calls, storage operations, queue sends/receives). At minimum the telemetry entry must record " +
            "the dependency name, operation name, duration, success/failure indicator, and correlation " +
            "identifier. See GlobalLoggingStandards.md Section 6.";

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
                    // Microsoft.Extensions.Logging.Abstractions is not referenced by this compilation;
                    // there is no way for this method to comply, so skip to avoid false positives.
                    return;
                }

                compilationContext.RegisterSyntaxNodeAction(
                    syntaxContext => AnalyzeMethod(syntaxContext, loggerGenericSymbol, loggerSymbol),
                    SyntaxKind.MethodDeclaration);
            });
        }

        private static void AnalyzeMethod(
            SyntaxNodeAnalysisContext context,
            INamedTypeSymbol? loggerGenericSymbol,
            INamedTypeSymbol? loggerSymbol)
        {
            var method = (MethodDeclarationSyntax)context.Node;

            SyntaxNode? body = (SyntaxNode?)method.Body ?? method.ExpressionBody;

            if (body is null)
            {
                return;
            }

            var invocations = body.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().ToList();

            InvocationExpressionSyntax? dependencyCall = invocations.FirstOrDefault(invocation =>
                invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                LoggingWellKnownTypes.DependencyCallMethodNames.Contains(memberAccess.Name.Identifier.ValueText, System.StringComparer.Ordinal));

            if (dependencyCall is null)
            {
                return;
            }

            bool hasLoggerCall = invocations.Any(invocation =>
            {
                if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                {
                    return false;
                }

                if (!LoggingWellKnownTypes.IsLoggerMethodName(memberAccess.Name.Identifier.ValueText))
                {
                    return false;
                }

                ISymbol? symbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol;

                return symbol is IMethodSymbol methodSymbol &&
                       methodSymbol.ReceiverType is not null &&
                       LoggingWellKnownTypes.IsLoggerReceiverType(methodSymbol.ReceiverType, loggerGenericSymbol, loggerSymbol);
            });

            if (hasLoggerCall)
            {
                return;
            }

            var memberAccessSyntax = (MemberAccessExpressionSyntax)dependencyCall.Expression;
            string methodName = method.Identifier.ValueText;
            string dependencyMethodName = memberAccessSyntax.Name.Identifier.ValueText;

            var diagnostic = Diagnostic.Create(Rule, memberAccessSyntax.Name.GetLocation(), methodName, dependencyMethodName);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
