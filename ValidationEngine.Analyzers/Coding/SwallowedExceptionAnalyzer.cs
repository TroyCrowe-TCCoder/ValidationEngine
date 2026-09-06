using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Coding
{
    /// <summary>
    /// Enforces GlobalCodingStandards.md coding.7.3: exceptions must not be caught and discarded
    /// silently. Every caught exception must be logged or rethrown. Flags empty catch blocks and
    /// catch blocks with no <c>Log*</c> call and no <c>throw</c>/<c>throw;</c> statement.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class SwallowedExceptionAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CODE018";

        private const string LogMethodNamePrefix = "Log";

        private static readonly LocalizableString Title =
            "Caught exceptions must not be swallowed silently";

        private static readonly LocalizableString MessageFormat =
            "Catch block for '{0}' neither logs nor rethrows the exception; every caught exception must be logged or rethrown, per GlobalCodingStandards.md coding.7.3";

        private static readonly LocalizableString Description =
            "Exceptions must not be caught and discarded silently. Every caught exception must be logged or " +
            "rethrown. See GlobalCodingStandards.md Section 7.3.";

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
                compilationContext.RegisterSyntaxNodeAction(AnalyzeCatchClause, SyntaxKind.CatchClause);
            });
        }

        private static void AnalyzeCatchClause(SyntaxNodeAnalysisContext context)
        {
            var catchClause = (CatchClauseSyntax)context.Node;

            if (catchClause.Block is null)
            {
                return;
            }

            if (HasThrowStatement(catchClause.Block) || HasLogInvocation(catchClause.Block))
            {
                return;
            }

            string exceptionTypeName = catchClause.Declaration?.Type?.ToString() ?? "Exception";

            var diagnostic = Diagnostic.Create(Rule, catchClause.CatchKeyword.GetLocation(), exceptionTypeName);
            context.ReportDiagnostic(diagnostic);
        }

        private static bool HasThrowStatement(BlockSyntax block)
        {
            return block.DescendantNodes().OfType<ThrowStatementSyntax>().Any() ||
                   block.DescendantNodes().OfType<ThrowExpressionSyntax>().Any();
        }

        private static bool HasLogInvocation(BlockSyntax block)
        {
            foreach (InvocationExpressionSyntax invocation in block.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                string? methodName = invocation.Expression switch
                {
                    MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
                    IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                    _ => null,
                };

                if (methodName is not null && methodName.StartsWith(LogMethodNamePrefix, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
