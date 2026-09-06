using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Coding
{
    /// <summary>
    /// Enforces GlobalCodingStandards.md coding.8.5: library code (NuGet packages, shared class
    /// libraries) must use 'ConfigureAwait(false)' on all 'await' calls to avoid capturing the
    /// synchronization context. Scoped to class-library projects (compilations that produce a
    /// dynamically linked library) since application projects do not require this call.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ConfigureAwaitAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CODE023";

        private const string ConfigureAwaitMethodName = "ConfigureAwait";

        private static readonly LocalizableString Title =
            "Library code must use ConfigureAwait(false) on all await calls";

        private static readonly LocalizableString MessageFormat =
            "'await' expression does not call ConfigureAwait(false); library code must use ConfigureAwait(false) on all await calls to avoid capturing the synchronization context, per GlobalCodingStandards.md coding.8.5";

        private static readonly LocalizableString Description =
            "Library code (NuGet packages, shared class libraries) must use ConfigureAwait(false) on " +
            "all await calls to avoid capturing the synchronization context. Application code (API " +
            "projects, web projects) does not require ConfigureAwait(false) because ASP.NET Core does " +
            "not use a synchronization context. See GlobalCodingStandards.md Section 8.5.";

        private const string Category = "Coding";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: "https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalCodingStandards.md");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(compilationContext =>
            {
                if (compilationContext.Compilation.Options.OutputKind != OutputKind.DynamicallyLinkedLibrary)
                {
                    return;
                }

                compilationContext.RegisterSyntaxNodeAction(AnalyzeAwaitExpression, SyntaxKind.AwaitExpression);
            });
        }

        private static void AnalyzeAwaitExpression(SyntaxNodeAnalysisContext context)
        {
            var awaitExpression = (AwaitExpressionSyntax)context.Node;

            if (HasConfigureAwaitCall(awaitExpression.Expression))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, awaitExpression.GetLocation()));
        }

        private static bool HasConfigureAwaitCall(ExpressionSyntax expression)
        {
            return expression is InvocationExpressionSyntax invocation &&
                   invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                   string.Equals(memberAccess.Name.Identifier.ValueText, ConfigureAwaitMethodName, System.StringComparison.Ordinal);
        }
    }
}
