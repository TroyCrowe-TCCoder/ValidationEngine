using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Coding
{
    /// <summary>
    /// Enforces GlobalCodingStandards.md coding.7.5/coding.7.7: unhandled exceptions must not
    /// surface raw stack traces to clients, and user-facing error messages must not expose
    /// internal details, exception messages, or stack traces. Flags <c>ex.ToString()</c>,
    /// <c>ex.StackTrace</c>, and <c>ex.Message</c> values returned directly in a controller action
    /// catch block's API response.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ExceptionDetailsExposedAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CODE020";

        private const string StackTracePropertyName = "StackTrace";
        private const string MessagePropertyName = "Message";
        private const string ToStringMethodName = "ToString";
        private const string ControllerSuffix = "Controller";

        private static readonly LocalizableString Title =
            "Exception details must not be exposed to clients";

        private static readonly LocalizableString MessageFormat =
            "'{0}' returns raw exception details ('{1}') directly in an API response; unhandled exceptions must not surface stack traces or exception messages to clients, per GlobalCodingStandards.md coding.7.5/coding.7.7";

        private static readonly LocalizableString Description =
            "Unhandled exceptions must not surface raw stack traces to clients. User-facing error messages " +
            "must not expose internal details, exception messages, or stack traces. See GlobalCodingStandards.md " +
            "Sections 7.5 and 7.7.";

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

            string? exceptionIdentifier = catchClause.Declaration?.Identifier.ValueText;

            if (string.IsNullOrEmpty(exceptionIdentifier) || catchClause.Block is null)
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
                if (returnStatement.Expression is null)
                {
                    continue;
                }

                foreach (MemberAccessExpressionSyntax memberAccess in
                    returnStatement.Expression.DescendantNodesAndSelf().OfType<MemberAccessExpressionSyntax>())
                {
                    if (memberAccess.Expression is not IdentifierNameSyntax identifier ||
                        !string.Equals(identifier.Identifier.ValueText, exceptionIdentifier, System.StringComparison.Ordinal))
                    {
                        continue;
                    }

                    string memberName = memberAccess.Name.Identifier.ValueText;
                    bool isExposedMember =
                        string.Equals(memberName, StackTracePropertyName, System.StringComparison.Ordinal) ||
                        string.Equals(memberName, MessagePropertyName, System.StringComparison.Ordinal) ||
                        string.Equals(memberName, ToStringMethodName, System.StringComparison.Ordinal);

                    if (!isExposedMember)
                    {
                        continue;
                    }

                    var diagnostic = Diagnostic.Create(
                        Rule,
                        memberAccess.GetLocation(),
                        containingType!.Identifier.ValueText,
                        $"{exceptionIdentifier}.{memberName}");
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private static bool IsControllerType(TypeDeclarationSyntax? containingType)
        {
            return containingType is not null &&
                   containingType.Identifier.ValueText.EndsWith(ControllerSuffix, System.StringComparison.Ordinal);
        }
    }
}
