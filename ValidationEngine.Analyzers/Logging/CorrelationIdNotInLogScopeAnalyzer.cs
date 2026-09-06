using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Logging
{
    /// <summary>
    /// Enforces GlobalLoggingStandards.md logging.4.2: the correlation identifier must be pushed
    /// into the log scope (via <c>ILogger.BeginScope</c>) for the lifetime of the request so that
    /// every log entry written during request processing includes it. Reported as a
    /// compilation-end diagnostic because the check spans the whole composition root.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CorrelationIdNotInLogScopeAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "LOG009";

        private static readonly LocalizableString Title =
            "Correlation identifier must be pushed into the log scope";

        private static readonly LocalizableString MessageFormat =
            "The compilation assigns HttpContext.TraceIdentifier but never calls ILogger.BeginScope; push the correlation identifier into the log scope for the request lifetime, per GlobalLoggingStandards.md logging.4.2";

        private static readonly LocalizableString Description =
            "The correlation identifier must be pushed into the log scope for the lifetime of the request, using " +
            "ILogger.BeginScope with a CorrelationId key, so that every log entry written during request " +
            "processing includes it. See GlobalLoggingStandards.md Section 4.2.";

        private const string Category = "Logging";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: "https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalLoggingStandards.md",
            customTags: WellKnownDiagnosticTags.CompilationEnd);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(compilationContext =>
            {
                INamedTypeSymbol? httpContextSymbol =
                    compilationContext.Compilation.GetTypeByMetadataName(LoggingWellKnownTypes.HttpContextMetadataName);

                if (httpContextSymbol is null)
                {
                    // Not an ASP.NET Core application; nothing to check.
                    return;
                }

                var tracker = new ScopeTracker();

                compilationContext.RegisterSyntaxNodeAction(
                    syntaxContext => AnalyzeAssignment(syntaxContext, tracker),
                    SyntaxKind.SimpleAssignmentExpression);

                compilationContext.RegisterSyntaxNodeAction(
                    syntaxContext => AnalyzeInvocation(syntaxContext, tracker),
                    SyntaxKind.InvocationExpression);

                compilationContext.RegisterCompilationEndAction(
                    compilationEndContext => AnalyzeCompilationEnd(compilationEndContext, tracker));
            });
        }

        private static void AnalyzeAssignment(SyntaxNodeAnalysisContext context, ScopeTracker tracker)
        {
            var assignment = (AssignmentExpressionSyntax)context.Node;

            if (assignment.Left is MemberAccessExpressionSyntax memberAccess &&
                string.Equals(memberAccess.Name.Identifier.ValueText, LoggingWellKnownTypes.TraceIdentifierPropertyName, System.StringComparison.Ordinal))
            {
                tracker.AssignsTraceIdentifier = true;
            }
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context, ScopeTracker tracker)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            if (LoggingWellKnownTypes.IsInvocationOfMember(invocation, LoggingWellKnownTypes.BeginScopeMethodName))
            {
                tracker.CallsBeginScope = true;
            }
        }

        private static void AnalyzeCompilationEnd(CompilationAnalysisContext context, ScopeTracker tracker)
        {
            if (!tracker.AssignsTraceIdentifier || tracker.CallsBeginScope)
            {
                return;
            }

            var diagnostic = Diagnostic.Create(Rule, Location.None);
            context.ReportDiagnostic(diagnostic);
        }

        private sealed class ScopeTracker
        {
            public bool AssignsTraceIdentifier;
            public bool CallsBeginScope;
        }
    }
}
