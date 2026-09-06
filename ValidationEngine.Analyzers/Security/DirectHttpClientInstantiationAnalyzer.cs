using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Security
{
    /// <summary>
    /// Enforces GlobalSecurityStandards.md security.8.6: all application-to-application HTTP
    /// calls must be made through the HttpClientManager library. Direct instantiation of
    /// 'HttpClient' in application code for in-scope calls is not permitted. Flags
    /// 'new HttpClient(...)' object creation expressions.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DirectHttpClientInstantiationAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SEC007";

        private static readonly LocalizableString Title =
            "HttpClient must not be instantiated directly; use HttpClientManager";

        private static readonly LocalizableString MessageFormat =
            "'HttpClient' must not be instantiated directly; application-to-application HTTP calls must be made through HttpClientManager, per GlobalSecurityStandards.md security.8.6";

        private static readonly LocalizableString Description =
            "All application-to-application HTTP calls must be made through the HttpClientManager " +
            "library, which enforces HTTPS-only base paths, bearer-token header-injection validation, " +
            "URI scheme allowlisting, and response size limits. Direct instantiation of HttpClient in " +
            "application code for in-scope calls is not permitted. Azure SDK clients are out of scope. " +
            "See GlobalSecurityStandards.md Section 8.6.";

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
                compilationContext.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
            });
        }

        private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
        {
            var creation = (ObjectCreationExpressionSyntax)context.Node;

            if (context.SemanticModel.GetSymbolInfo(creation, context.CancellationToken).Symbol is not IMethodSymbol constructorSymbol)
            {
                return;
            }

            string typeMetadataName = constructorSymbol.ContainingType?.ToDisplayString() ?? string.Empty;

            if (typeMetadataName != SecurityWellKnownTypes.HttpClientMetadataName)
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, creation.GetLocation()));
        }
    }
}
