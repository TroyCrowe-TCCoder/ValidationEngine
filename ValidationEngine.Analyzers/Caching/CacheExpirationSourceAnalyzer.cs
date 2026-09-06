using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Caching
{
    /// <summary>
    /// Enforces GlobalCachingStandards.md caching.6.3: time-bounded cache expiration must be
    /// sourced from IOptionsMonitor&lt;CacheOptions&gt;.CurrentValue. IOptions&lt;T&gt; must not be
    /// used (it is a startup snapshot that never observes configuration changes), and hardcoded
    /// TimeSpan literals must not be passed directly into SetAbsoluteExpiration.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CacheExpirationSourceAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CACHE006";

        private const string IOptionsGenericName = "IOptions";
        private const string SetAbsoluteExpirationMethodName = "SetAbsoluteExpiration";
        private const string TimeSpanTypeName = "TimeSpan";
        private const string CurrentValuePropertyName = "CurrentValue";

        private static readonly LocalizableString Title =
            "Cache expiration must be sourced from IOptionsMonitor<CacheOptions>, not IOptions<T> or a hardcoded value";

        private static readonly LocalizableString MessageFormat = "{0}";

        private static readonly LocalizableString Description =
            "Time-bounded cache entries must read their expiration from IOptionsMonitor<CacheOptions>.CurrentValue. " +
            "IOptions<T> is a startup snapshot and does not pick up configuration changes without an application " +
            "restart; hardcoded TimeSpan literals bypass configuration entirely. See GlobalCachingStandards.md Section 6.3.";

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

            context.RegisterSyntaxNodeAction(AnalyzeGenericName, SyntaxKind.GenericName);
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeGenericName(SyntaxNodeAnalysisContext context)
        {
            var genericName = (GenericNameSyntax)context.Node;

            if (genericName.Identifier.ValueText != IOptionsGenericName)
            {
                return;
            }

            if (genericName.TypeArgumentList.Arguments.Count != 1)
            {
                return;
            }

            string typeArgumentName = genericName.TypeArgumentList.Arguments[0] switch
            {
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
                _ => string.Empty,
            };

            if (!typeArgumentName.EndsWith(CachingWellKnownTypes.CacheOptionsClassNameSuffix, StringComparison.Ordinal))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                genericName.GetLocation(),
                $"'{genericName}' uses IOptions<{typeArgumentName}> instead of IOptionsMonitor<{typeArgumentName}>; IOptions<T> is a startup snapshot and will not observe configuration changes, per GlobalCachingStandards.md caching.6.3"));
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess ||
                memberAccess.Name.Identifier.ValueText != SetAbsoluteExpirationMethodName)
            {
                return;
            }

            foreach (ArgumentSyntax argument in invocation.ArgumentList.Arguments)
            {
                if (!IsTimeSpanFromInvocation(argument.Expression))
                {
                    continue;
                }

                if (ReferencesCurrentValue(argument.Expression))
                {
                    continue;
                }

                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    argument.Expression.GetLocation(),
                    $"SetAbsoluteExpiration is passed a hardcoded '{argument.Expression}' value instead of an expiration sourced from IOptionsMonitor<CacheOptions>.CurrentValue, per GlobalCachingStandards.md caching.6.3"));
            }
        }

        private static bool IsTimeSpanFromInvocation(ExpressionSyntax expression)
        {
            return expression is InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax
                {
                    Expression: IdentifierNameSyntax { Identifier.ValueText: TimeSpanTypeName },
                },
            };
        }

        private static bool ReferencesCurrentValue(ExpressionSyntax expression)
        {
            return expression.DescendantNodesAndSelf()
                .OfType<MemberAccessExpressionSyntax>()
                .Any(m => m.Name.Identifier.ValueText == CurrentValuePropertyName);
        }
    }
}
