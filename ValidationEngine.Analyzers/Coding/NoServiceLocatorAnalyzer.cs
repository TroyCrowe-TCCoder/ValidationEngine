using System.Collections.Immutable;
using System.Linq;
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
    /// (the service-locator anti-pattern). Calls made directly within a test method (decorated
    /// with xUnit '[Fact]'/'[Theory]' or MSTest '[TestMethod]') are exempt: building a small,
    /// local <c>ServiceProvider</c> in a test to satisfy a constructor dependency is a standard
    /// test-arrangement pattern, not the runtime service-locator anti-pattern the rule targets.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class NoServiceLocatorAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CODE015";

        private static readonly ImmutableArray<string> TestMethodAttributeNames = ImmutableArray.Create(
            "Fact",
            "FactAttribute",
            "Theory",
            "TheoryAttribute",
            "TestMethod",
            "TestMethodAttribute");

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

            if (IsInsideServiceProviderFactoryDelegate(invocation, context.SemanticModel, serviceProviderSymbol, context.CancellationToken))
            {
                // Resolution inside a DI registration factory delegate (e.g.
                // services.AddScoped<TInterface>(provider => ...)) is a composition-root
                // concern, not the service-locator anti-pattern: the delegate itself is only
                // ever invoked by the container when building the graph, and this is the
                // standard ASP.NET Core decorator/proxy registration pattern.
                return;
            }

            if (IsInsideTestMethod(invocation))
            {
                // Resolution directly inside a test method is standard test arrangement (e.g.
                // building a local ServiceProvider to satisfy a constructor dependency), not the
                // runtime service-locator anti-pattern this rule targets.
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

        private static bool IsInsideServiceProviderFactoryDelegate(
            InvocationExpressionSyntax invocation,
            SemanticModel semanticModel,
            INamedTypeSymbol serviceProviderSymbol,
            System.Threading.CancellationToken cancellationToken)
        {
            foreach (SyntaxNode ancestor in invocation.Ancestors())
            {
                if (ancestor is not (SimpleLambdaExpressionSyntax or ParenthesizedLambdaExpressionSyntax or AnonymousMethodExpressionSyntax))
                {
                    continue;
                }

                if (semanticModel.GetSymbolInfo(ancestor, cancellationToken).Symbol is not IMethodSymbol lambdaSymbol ||
                    lambdaSymbol.Parameters.Length == 0)
                {
                    continue;
                }

                ITypeSymbol parameterType = lambdaSymbol.Parameters[0].Type;

                if (SymbolEqualityComparer.Default.Equals(parameterType, serviceProviderSymbol) ||
                    ImplementsServiceProvider(parameterType, serviceProviderSymbol))
                {
                    return true;
                }
            }

            return false;
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

        private static bool IsInsideTestMethod(InvocationExpressionSyntax invocation)
        {
            MethodDeclarationSyntax? method = invocation
                .Ancestors()
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault();

            if (method is null)
            {
                return false;
            }

            foreach (AttributeListSyntax attributeList in method.AttributeLists)
            {
                foreach (AttributeSyntax attribute in attributeList.Attributes)
                {
                    string attributeName = attribute.Name.ToString();
                    string simpleName = attributeName.Contains('.')
                        ? attributeName.Substring(attributeName.LastIndexOf('.') + 1)
                        : attributeName;

                    if (TestMethodAttributeNames.Contains(simpleName))
                    {
                        return true;
                    }
                }
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
