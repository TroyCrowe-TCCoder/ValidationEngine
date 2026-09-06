using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Coding
{
    /// <summary>
    /// Enforces GlobalCodingStandards.md coding.3.4 / coding.7.2: guard clauses must appear at the
    /// top of every method that has preconditions. Flags methods where a guard-clause-shaped
    /// statement (an <c>if</c> whose body throws or returns) appears after one or more non-guard
    /// statements, indicating precondition validation was deferred past the happy-path start.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DeferredGuardClauseAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CODE008";

        private static readonly LocalizableString Title =
            "Guard clauses must appear at the top of the method";

        private static readonly LocalizableString MessageFormat =
            "Guard clause in method '{0}' appears after other statements; move precondition validation to the top of the method before the happy path, per GlobalCodingStandards.md coding.3.4";

        private static readonly LocalizableString Description =
            "Guard clauses must appear at the top of every method that has preconditions. A guard clause " +
            "validates a single precondition and immediately throws or returns before any business logic " +
            "executes. Precondition validation must not be deferred to deep implementation code. " +
            "See GlobalCodingStandards.md Section 3.4.";

        private const string Category = "Coding";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
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
                compilationContext.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
            });
        }

        private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            var method = (MethodDeclarationSyntax)context.Node;

            if (method.Body is null)
            {
                return;
            }

            bool sawNonGuardStatement = false;

            foreach (StatementSyntax statement in method.Body.Statements)
            {
                bool isGuardClause = CodingWellKnownTypes.IsGuardClauseStatement(statement);

                if (isGuardClause)
                {
                    if (sawNonGuardStatement)
                    {
                        var diagnostic = Diagnostic.Create(
                            Rule,
                            statement.GetLocation(),
                            method.Identifier.ValueText);

                        context.ReportDiagnostic(diagnostic);
                    }

                    continue;
                }

                sawNonGuardStatement = true;
            }
        }
    }
}
