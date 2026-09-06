using System;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Logging
{
    /// <summary>
    /// Enforces GlobalLoggingStandards.md logging.7: every hosted service must expose a
    /// <c>/healthcheck</c> liveness endpoint registered via
    /// <c>app.MapHealthChecks("/healthcheck")</c>. Reported as a compilation-end diagnostic because
    /// the check spans the whole composition root, not a single syntax node.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MissingHealthCheckEndpointAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "LOG014";

        private static readonly LocalizableString Title =
            "Program.cs must expose a /healthcheck endpoint";

        private static readonly LocalizableString MessageFormat =
            "'{0}' does not call MapHealthChecks(\"/healthcheck\"); every hosted service must expose a liveness endpoint, per GlobalLoggingStandards.md logging.7";

        private static readonly LocalizableString Description =
            "Every hosted service must expose a /healthcheck endpoint that confirms the process is running and " +
            "not deadlocked, registered via app.MapHealthChecks(\"/healthcheck\"). See GlobalLoggingStandards.md Section 7.";

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
                var tracker = new RegistrationTracker();

                compilationContext.RegisterSyntaxNodeAction(
                    syntaxContext => AnalyzeInvocation(syntaxContext, tracker),
                    SyntaxKind.InvocationExpression);

                compilationContext.RegisterCompilationEndAction(
                    compilationEndContext => AnalyzeCompilationEnd(compilationEndContext, tracker));
            });
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context, RegistrationTracker tracker)
        {
            if (tracker.Found)
            {
                return;
            }

            var invocation = (InvocationExpressionSyntax)context.Node;

            if (!LoggingWellKnownTypes.IsInvocationOfMember(invocation, LoggingWellKnownTypes.MapHealthChecksMethodName))
            {
                return;
            }

            foreach (ArgumentSyntax argument in invocation.ArgumentList.Arguments)
            {
                if (argument.Expression is LiteralExpressionSyntax literal &&
                    string.Equals(literal.Token.ValueText, LoggingWellKnownTypes.HealthCheckEndpointRoute, StringComparison.Ordinal))
                {
                    tracker.Found = true;
                    return;
                }
            }
        }

        private static void AnalyzeCompilationEnd(CompilationAnalysisContext context, RegistrationTracker tracker)
        {
            if (tracker.Found)
            {
                return;
            }

            foreach (SyntaxTree tree in context.Compilation.SyntaxTrees)
            {
                string fileName = Path.GetFileName(tree.FilePath);

                if (!string.Equals(fileName, LoggingWellKnownTypes.ProgramFileName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var diagnostic = Diagnostic.Create(Rule, Location.None, fileName);
                context.ReportDiagnostic(diagnostic);
                return;
            }
        }

        private sealed class RegistrationTracker
        {
            public bool Found;
        }
    }
}
