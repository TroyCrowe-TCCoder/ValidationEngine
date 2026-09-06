using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Coding
{
    /// <summary>
    /// Enforces GlobalCodingStandards.md coding.4.5: dependencies must be registered in the DI
    /// container at the application entry point, service locator patterns must not be used inside
    /// services, and dependencies must be injected via constructor. Flags
    /// <c>IServiceProvider.GetService</c>/<c>GetRequiredService</c> calls made outside Program.cs
    /// (the service-locator anti-pattern).
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class NoServiceLocatorAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CODE015";

        private static readonly LocalizableString Title =
            "Service locator pattern must not be used";

        private static readonly LocalizableString MessageFormat =
            "'{0}' resolves '{1}' via the service locator pattern; dependencies must be injected via the constructor and only registered/resolved at the application entry point, per GlobalCodingStandards.md coding.4.5";

        private static readonly LocalizableString Description =
            "Dependencies must be registered in the DI container at the application entry point. Service " +
            "locator patterns must not be used inside services. Dependencies must be injected via constructor. " +
            "See GlobalCodingStandards.md Section 4.5.";

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
                INamedTypeSymbol? serviceProviderSymbol = compilationContext.Compilation.GetTypeByMetadataName(
                    CodingWellKnownTypes.IServiceProviderMetadataName);

                if (serviceProviderSymbol is null)
                {
                    return;
                }

                compilationContext.RegisterSyntaxNodeAction(
                    syntaxContext => AnalyzeInvocation(syntaxContext, serviceProviderSymbol),
                    SyntaxKind.InvocationExpression);
            });
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context, INamedTypeSymbol serviceProviderSymbol)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            bool isGetService = CodingWellKnownTypes.IsInvocationOfMember(invocation, CodingWellKnownTypes.GetServiceMethodName);
            bool isGetRequiredService = CodingWellKnownTypes.IsInvocationOfMember(invocation, CodingWellKnownTypes.GetRequiredServiceMethodName);

            if (!isGetService && !isGetRequiredService)
            {
                return;
            }

            string fileName = System.IO.Path.GetFileName(invocation.SyntaxTree.FilePath);

            if (CodingWellKnownTypes.IsProgramFile(fileName))
            {
                // Composition-root resolution at the application entry point is expected.
                return;
            }

            if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol methodSymbol)
            {
                return;
            }

            if (!IsServiceProviderMember(methodSymbol, serviceProviderSymbol))
            {
                return;
            }

            TypeDeclarationSyntax? containingType = CodingWellKnownTypes.GetContainingType(invocation);
            string containingTypeName = containingType?.Identifier.ValueText ?? fileName;
            string methodName = isGetRequiredService
                ? CodingWellKnownTypes.GetRequiredServiceMethodName
                : CodingWellKnownTypes.GetServiceMethodName;

            var diagnostic = Diagnostic.Create(Rule, invocation.GetLocation(), containingTypeName, methodName);
            context.ReportDiagnostic(diagnostic);
        }

        private static bool IsServiceProviderMember(IMethodSymbol methodSymbol, INamedTypeSymbol serviceProviderSymbol)
        {
            IMethodSymbol targetMethod = methodSymbol.ReducedFrom ?? methodSymbol;

            if (targetMethod.ContainingType is not null &&
                SymbolEqualityComparer.Default.Equals(targetMethod.ContainingType, serviceProviderSymbol))
            {
                return true;
            }

            if (targetMethod.IsExtensionMethod && targetMethod.Parameters.Length > 0)
            {
                ITypeSymbol receiverType = targetMethod.Parameters[0].Type;
                return ImplementsServiceProvider(receiverType, serviceProviderSymbol);
            }

            return false;
        }

        private static bool ImplementsServiceProvider(ITypeSymbol receiverType, INamedTypeSymbol serviceProviderSymbol)
        {
            if (SymbolEqualityComparer.Default.Equals(receiverType, serviceProviderSymbol))
            {
                return true;
            }

            foreach (INamedTypeSymbol iface in receiverType.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(iface, serviceProviderSymbol))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
