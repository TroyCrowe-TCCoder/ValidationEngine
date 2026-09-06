using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Security
{
    /// <summary>
    /// Enforces GlobalSecurityStandards.md security.3.6: the default 'AddInMemoryTokenCaches()'
    /// registration must be replaced with 'AddDistributedTokenCaches()' backed by Redis. The
    /// in-memory cache does not survive application restarts or scale-out to multiple instances.
    /// Flags any 'AddInMemoryTokenCaches()' invocation.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class InMemoryTokenCacheAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SEC004";

        private static readonly LocalizableString Title =
            "In-memory token cache must not be used";

        private static readonly LocalizableString MessageFormat =
            "'AddInMemoryTokenCaches()' is registered; it must be replaced with 'AddDistributedTokenCaches()' backed by Redis, per GlobalSecurityStandards.md security.3.6";

        private static readonly LocalizableString Description =
            "The default AddInMemoryTokenCaches() registration must be replaced with " +
            "AddDistributedTokenCaches() backed by Redis. The in-memory cache does not survive application " +
            "restarts or scale-out to multiple instances. See GlobalSecurityStandards.md Section 3.6.";

        private const string Category = "Security";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
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
                compilationContext.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
            });
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            if (!SecurityWellKnownTypes.IsInvocationOfMember(invocation, SecurityWellKnownTypes.AddInMemoryTokenCachesMethodName))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
        }
    }
}
