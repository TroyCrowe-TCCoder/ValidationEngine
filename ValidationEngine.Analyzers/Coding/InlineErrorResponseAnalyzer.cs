using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Coding
{
    /// <summary>
    /// Enforces GlobalCodingStandards.md coding.7.4: API error responses must be formatted using
    /// ProblemDetails middleware or a global exception handler, not inline in individual
    /// controllers. Flags controller action catch blocks that return a manually constructed
    /// error-response object (a non-ProblemDetails object creation or anonymous object) instead of
    /// relying on the built-in Problem()/ValidationProblem() helpers or the global handler.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class InlineErrorResponseAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CODE019";

        private const string ProblemDetailsTypeName = "ProblemDetails";

        private static readonly LocalizableString Title =
            "API error responses must not be formatted inline in controllers";

        private static readonly LocalizableString MessageFormat =
            "'{0}' manually constructs an error-response object in a catch block; use ProblemDetails/Problem()/ValidationProblem() or rely on the global exception handler instead of formatting errors inline, per GlobalCodingStandards.md coding.7.4";

        private static readonly LocalizableString Description =
            "API error responses must be formatted using ProblemDetails middleware or a global exception " +
            "handler. Error responses must not be formatted inline in individual controllers. See " +
            "GlobalCodingStandards.md Section 7.4.";

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

            TypeDeclarationSyntax? containingType = CodingWellKnownTypes.GetContainingType(catchClause);

            if (!IsControllerType(containingType))
            {
                return;
            }

            foreach (ReturnStatementSyntax returnStatement in catchClause.Block.DescendantNodes().OfType<ReturnStatementSyntax>())
            {
                ArgumentListSyntax? argumentList = returnStatement.Expression switch
                {
                    InvocationExpressionSyntax invocation => invocation.ArgumentList,
                    ObjectCreationExpressionSyntax objectCreation => objectCreation.ArgumentList,
                    _ => null,
                };

                if (argumentList is null)
                {
                    continue;
                }

                foreach (ArgumentSyntax argument in argumentList.Arguments)
                {
                    if (!IsManualErrorResponseObject(argument.Expression))
                    {
                        continue;
                    }

                    var diagnostic = Diagnostic.Create(Rule, argument.Expression.GetLocation(), containingType!.Identifier.ValueText);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private static bool IsControllerType(TypeDeclarationSyntax? containingType)
        {
            const string ControllerSuffix = "Controller";

            return containingType is not null &&
                   containingType.Identifier.ValueText.EndsWith(ControllerSuffix, System.StringComparison.Ordinal);
        }

        private static bool IsManualErrorResponseObject(ExpressionSyntax expression)
        {
            if (expression is AnonymousObjectCreationExpressionSyntax)
            {
                return true;
            }

            if (expression is not ObjectCreationExpressionSyntax objectCreation)
            {
                return false;
            }

            string? typeName = objectCreation.Type switch
            {
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
                _ => null,
            };

            return typeName is not null &&
                   !string.Equals(typeName, ProblemDetailsTypeName, System.StringComparison.Ordinal);
        }
    }
}
