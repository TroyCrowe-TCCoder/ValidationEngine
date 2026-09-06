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
    /// Enforces GlobalLoggingStandards.md logging.2.4: when a Program.cs file is present, the
    /// compilation must configure a logging destination by registering Application Insights
    /// telemetry (AddApplicationInsightsTelemetry) somewhere in the composition root.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ProgramLoggingDestinationAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "LOG004";

        private static readonly LocalizableString Title =
            "Program.cs must configure a logging destination";

        private static readonly LocalizableString MessageFormat =
            "'{0}' does not register Application Insights telemetry (AddApplicationInsightsTelemetry); the logging destination is not configured, per GlobalLoggingStandards.md logging.2.4";

        private static readonly LocalizableString Description =
            "Applications must configure at least one logging destination in the composition root. " +
            "When a Program.cs file is present, AddApplicationInsightsTelemetry (or an equivalent " +
            "registration) must be called. See GlobalLoggingStandards.md Section 2.4.";

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

            if (LoggingWellKnownTypes.IsInvocationOfMember(invocation, LoggingWellKnownTypes.ApplicationInsightsRegistrationMethodName))
            {
                tracker.Found = true;
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
