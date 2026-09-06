using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Logging
{
    /// <summary>
    /// Enforces GlobalLoggingStandards.md logging.4.1: every inbound HTTP request must have a
    /// correlation identifier attached at the application entry point (typically by assigning
    /// <c>HttpContext.TraceIdentifier</c>) before any log entries are written. Reported as a
    /// compilation-end diagnostic because the check spans the whole composition root, not a single
    /// syntax node.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CorrelationIdNotAttachedAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "LOG008";

        private static readonly LocalizableString Title =
            "Inbound requests must have a correlation identifier attached";

        private static readonly LocalizableString MessageFormat =
            "The compilation references HttpContext but never assigns TraceIdentifier; attach a correlation identifier at the application entry point, per GlobalLoggingStandards.md logging.4.1";

        private static readonly LocalizableString Description =
            "Every inbound HTTP request must have a correlation identifier attached at the application entry " +
            "point before any log entries are written, typically by assigning HttpContext.TraceIdentifier from " +
            "the X-Correlation-Id or X-Request-Id header (or a newly generated Guid). See GlobalLoggingStandards.md Section 4.1.";

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

                var tracker = new AssignmentTracker();

                compilationContext.RegisterSyntaxNodeAction(
                    syntaxContext => AnalyzeAssignment(syntaxContext, tracker),
                    SyntaxKind.SimpleAssignmentExpression);

                compilationContext.RegisterCompilationEndAction(
                    compilationEndContext => AnalyzeCompilationEnd(compilationEndContext, tracker));
            });
        }

        private static void AnalyzeAssignment(SyntaxNodeAnalysisContext context, AssignmentTracker tracker)
        {
            if (tracker.Found)
            {
                return;
            }

            var assignment = (AssignmentExpressionSyntax)context.Node;

            if (assignment.Left is MemberAccessExpressionSyntax memberAccess &&
                string.Equals(memberAccess.Name.Identifier.ValueText, LoggingWellKnownTypes.TraceIdentifierPropertyName, System.StringComparison.Ordinal))
            {
                tracker.Found = true;
            }
        }

        private static void AnalyzeCompilationEnd(CompilationAnalysisContext context, AssignmentTracker tracker)
        {
            if (tracker.Found)
            {
                return;
            }

            var diagnostic = Diagnostic.Create(Rule, Location.None);
            context.ReportDiagnostic(diagnostic);
        }

        private sealed class AssignmentTracker
        {
            public bool Found;
        }
    }
}
