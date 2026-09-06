using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Security
{
    /// <summary>
    /// Enforces GlobalSecurityStandards.md security.3.1: every API endpoint must require a valid
    /// access token by default. Endpoints that opt out with '[AllowAnonymous]' must document the
    /// reason in a comment on the same line or directly above the attribute. Flags
    /// '[AllowAnonymous]' attributes with no adjacent explanatory comment.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UndocumentedAllowAnonymousAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SEC002";

        private static readonly LocalizableString Title =
            "AllowAnonymous must document the reason for the exception";

        private static readonly LocalizableString MessageFormat =
            "'[AllowAnonymous]' on '{0}' has no explanatory comment on the same line or directly above it, per GlobalSecurityStandards.md security.3.1";

        private static readonly LocalizableString Description =
            "Every API endpoint must require a valid access token by default. Endpoints that do not require " +
            "authentication must be explicitly opted out with [AllowAnonymous] and the reason must be " +
            "documented in a comment on the same line or directly above the attribute. See GlobalSecurityStandards.md " +
            "Section 3.1.";

        private const string Category = "Security";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: "https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalSecurityStandards.md");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(compilationContext =>
            {
                compilationContext.RegisterSyntaxNodeAction(AnalyzeAttribute, SyntaxKind.Attribute);
            });
        }

        private static void AnalyzeAttribute(SyntaxNodeAnalysisContext context)
        {
            var attribute = (AttributeSyntax)context.Node;

            if (!IsAllowAnonymousAttribute(attribute))
            {
                return;
            }

            if (HasAdjacentExplanatoryComment(attribute))
            {
                return;
            }

            string targetName = GetAttributeTargetName(attribute);

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                attribute.GetLocation(),
                targetName));
        }

        private static bool IsAllowAnonymousAttribute(AttributeSyntax attribute)
        {
            string name = attribute.Name switch
            {
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
                _ => attribute.Name.ToString(),
            };

            return string.Equals(name, SecurityWellKnownTypes.AllowAnonymousAttributeName, System.StringComparison.Ordinal);
        }

        private static bool HasAdjacentExplanatoryComment(AttributeSyntax attribute)
        {
            AttributeListSyntax? attributeList = attribute.Parent as AttributeListSyntax;
            SyntaxNode nodeForTrivia = (SyntaxNode?)attributeList ?? attribute;

            // Same-line trailing comment, e.g. `[AllowAnonymous] // reason`
            if (HasCommentTrivia(nodeForTrivia.GetTrailingTrivia()))
            {
                return true;
            }

            // Leading comment directly above the attribute list, e.g. `// reason` on the previous line.
            if (HasCommentTrivia(nodeForTrivia.GetLeadingTrivia()))
            {
                return true;
            }

            return false;
        }

        private static bool HasCommentTrivia(SyntaxTriviaList triviaList)
        {
            return triviaList.Any(trivia =>
                trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                trivia.IsKind(SyntaxKind.MultiLineCommentTrivia));
        }

        private static string GetAttributeTargetName(AttributeSyntax attribute)
        {
            SyntaxNode? current = attribute.Parent?.Parent;

            return current switch
            {
                MethodDeclarationSyntax method => method.Identifier.ValueText,
                TypeDeclarationSyntax type => type.Identifier.ValueText,
                _ => "target",
            };
        }
    }
}
