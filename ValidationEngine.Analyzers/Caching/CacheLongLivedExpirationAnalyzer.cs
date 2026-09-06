using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Caching
{
    /// <summary>
    /// Enforces GlobalCachingStandards.md caching.6.2: a cache entry authored using the long-lived
    /// pattern must not set an expiration and must be invalidated on write only. This analyzer
    /// implements the machine-readable long-lived signal approved in STD-013: a method marked with
    /// a <c>[LongLivedCache]</c> attribute is asserting that the entries it writes follow the
    /// long-lived lifecycle pattern (GlobalCachingStandards.md Section 6.1). Flags any
    /// SetAbsoluteExpiration/SetSlidingExpiration call, or AbsoluteExpiration/
    /// AbsoluteExpirationRelativeToNow/SlidingExpiration property assignment, made within a method
    /// carrying that attribute, since a long-lived entry must never have a time-based expiration.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CacheLongLivedExpirationAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CACHE016";

        private static readonly LocalizableString Title =
            "Methods marked [LongLivedCache] must not set a cache expiration";

        private static readonly LocalizableString MessageFormat =
            "'{0}' is marked [LongLivedCache] but sets an expiration via '{1}'; the long-lived pattern requires no expiration be set, invalidated on write only, per GlobalCachingStandards.md caching.6.2";

        private static readonly LocalizableString Description =
            "A cache entry authored using the long-lived pattern (GlobalCachingStandards.md Section 6.1 " +
            "\u2014 stable reference data that changes only on explicit administrator write) must not set a " +
            "time-based expiration. Setting an expiration on a [LongLivedCache]-marked method contradicts " +
            "the declared lifecycle and causes unnecessary database round-trips and a staleness window " +
            "that serves no purpose. See GlobalCachingStandards.md Section 6.2.";

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

            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeAssignment, SyntaxKind.SimpleAssignmentExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            {
                return;
            }

            string methodName = memberAccess.Name.Identifier.ValueText;

            if (methodName != CachingWellKnownTypes.SetAbsoluteExpirationMethodName &&
                methodName != CachingWellKnownTypes.SetSlidingExpirationMethodName)
            {
                return;
            }

            Report(context, invocation, methodName);
        }

        private static void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
        {
            var assignment = (AssignmentExpressionSyntax)context.Node;

            if (assignment.Left is not MemberAccessExpressionSyntax memberAccess)
            {
                return;
            }

            string propertyName = memberAccess.Name.Identifier.ValueText;

            if (propertyName != CachingWellKnownTypes.AbsoluteExpirationPropertyName &&
                propertyName != CachingWellKnownTypes.AbsoluteExpirationRelativeToNowPropertyName &&
                propertyName != CachingWellKnownTypes.SlidingExpirationPropertyName)
            {
                return;
            }

            Report(context, assignment, propertyName);
        }

        private static void Report(SyntaxNodeAnalysisContext context, SyntaxNode expirationNode, string memberName)
        {
            MethodDeclarationSyntax? containingMethod = CachingWellKnownTypes.GetContainingMethod(expirationNode);

            if (containingMethod is null)
            {
                return;
            }

            if (!CachingWellKnownTypes.HasLongLivedCacheAttribute(containingMethod, context.SemanticModel, context.CancellationToken))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                expirationNode.GetLocation(),
                containingMethod.Identifier.ValueText,
                memberName));
        }
    }
}
