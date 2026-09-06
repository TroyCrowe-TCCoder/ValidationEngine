using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Caching
{
    /// <summary>
    /// Enforces GlobalCachingStandards.md caching.4.1/5.1: every key-building method in the
    /// dedicated "CacheKeys" static class must accept a "tenantId" parameter and use it as the
    /// first segment of the returned key, without exception.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CacheKeyTenantIdSegmentAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CACHE005";

        private static readonly LocalizableString Title =
            "Cache key methods must include TenantId as the first key segment";

        private static readonly LocalizableString MessageFormat =
            "Cache key method '{0}' in the CacheKeys class does not have TenantId as the first key segment; TenantId is required as the first segment of every cache key per GlobalCachingStandards.md caching.4.1/5.1";

        private static readonly LocalizableString Description =
            "Every method in the CacheKeys class must accept a 'tenantId' parameter and use it as the " +
            "first segment of the returned cache key, even in applications that are not yet " +
            "multi-tenant. See GlobalCachingStandards.md Sections 4.1 and 5.1.";

        private const string Category = "Caching";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: "https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalCachingStandards.md");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
        }

        private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            var method = (MethodDeclarationSyntax)context.Node;

            if (method.Parent is not ClassDeclarationSyntax classDeclaration ||
                classDeclaration.Identifier.ValueText != CachingWellKnownTypes.CacheKeysClassName)
            {
                return;
            }

            if (method.ReturnType is not PredefinedTypeSyntax { Keyword.RawKind: (int)SyntaxKind.StringKeyword })
            {
                return;
            }

            ExpressionSyntax? keyExpression = GetReturnedExpression(method);

            if (keyExpression is null || !IsComposedKeyExpression(keyExpression))
            {
                // Only apply the heuristic to methods that visibly compose a segmented key from
                // interpolation/concatenation; skip delegation to helpers, string.Format, etc. to
                // avoid false positives on forms this analyzer cannot reliably parse.
                return;
            }

            ParameterSyntax? tenantIdParameter = method.ParameterList.Parameters
                .FirstOrDefault(p => p.Identifier.ValueText == CachingWellKnownTypes.TenantIdParameterName);

            if (tenantIdParameter is null)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, method.Identifier.GetLocation(), method.Identifier.ValueText));
                return;
            }

            string? firstSegmentIdentifier = GetFirstSegmentIdentifierName(keyExpression);

            if (firstSegmentIdentifier != CachingWellKnownTypes.TenantIdParameterName)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, keyExpression.GetLocation(), method.Identifier.ValueText));
            }
        }

        private static ExpressionSyntax? GetReturnedExpression(MethodDeclarationSyntax method)
        {
            if (method.ExpressionBody is { Expression: { } expressionBodyExpression })
            {
                return expressionBodyExpression;
            }

            if (method.Body is { } body)
            {
                var returnStatement = body.Statements.OfType<ReturnStatementSyntax>().FirstOrDefault();
                return returnStatement?.Expression;
            }

            return null;
        }

        private static bool IsComposedKeyExpression(ExpressionSyntax expression)
        {
            return expression is InterpolatedStringExpressionSyntax ||
                   expression is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.AddExpression };
        }

        private static string? GetFirstSegmentIdentifierName(ExpressionSyntax expression)
        {
            switch (expression)
            {
                case InterpolatedStringExpressionSyntax interpolated:
                    var firstContent = interpolated.Contents.FirstOrDefault();
                    return firstContent is InterpolationSyntax { Expression: IdentifierNameSyntax identifier }
                        ? identifier.Identifier.ValueText
                        : null;

                case BinaryExpressionSyntax { RawKind: (int)SyntaxKind.AddExpression } binary:
                    return GetFirstSegmentIdentifierName(binary.Left);

                case IdentifierNameSyntax leaf:
                    return leaf.Identifier.ValueText;

                default:
                    return null;
            }
        }
    }
}
