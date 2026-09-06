using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Coding
{
    /// <summary>
    /// Enforces GlobalCodingStandards.md coding.4.4: dependencies must be on abstractions where
    /// testability seams or substitutability justify an abstraction. Flags Service-suffixed classes
    /// that reference a concrete Repository-suffixed type (via field, property, or constructor
    /// parameter) when a corresponding interface abstraction (e.g. <c>IFooRepository</c> for
    /// <c>FooRepository</c>) is already declared in the compilation but not used.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ConcreteDependencyAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CODE014";

        private const string ServiceSuffix = "Service";
        private const string RepositorySuffix = "Repository";
        private const string InterfacePrefix = "I";

        private static readonly LocalizableString Title =
            "Depend on an abstraction when one is already available";

        private static readonly LocalizableString MessageFormat =
            "'{0}' depends on concrete type '{1}' directly, but '{2}' is already declared in the solution; depend on '{2}' instead so the dependency is substitutable and testable, per GlobalCodingStandards.md coding.4.4";

        private static readonly LocalizableString Description =
            "Dependencies must be on abstractions only where external dependencies, testability seams, or " +
            "substitutability justify an abstraction. When a matching interface abstraction already exists for " +
            "a repository dependency, it must be used instead of the concrete type. See GlobalCodingStandards.md Section 4.4.";

        private const string Category = "Coding";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: "https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalCodingStandards.md",
            customTags: WellKnownDiagnosticTags.CompilationEnd);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(compilationContext =>
            {
                var declaredInterfaceNames = new ConcurrentDictionary<string, byte>(System.StringComparer.Ordinal);
                var candidates = new ConcurrentBag<(Location Location, string ServiceClassName, string ConcreteTypeName)>();

                compilationContext.RegisterSyntaxNodeAction(syntaxContext =>
                {
                    var interfaceDeclaration = (InterfaceDeclarationSyntax)syntaxContext.Node;
                    declaredInterfaceNames.TryAdd(interfaceDeclaration.Identifier.ValueText, 0);
                }, SyntaxKind.InterfaceDeclaration);

                compilationContext.RegisterSyntaxNodeAction(syntaxContext => AnalyzeField(syntaxContext, candidates), SyntaxKind.FieldDeclaration);
                compilationContext.RegisterSyntaxNodeAction(syntaxContext => AnalyzeProperty(syntaxContext, candidates), SyntaxKind.PropertyDeclaration);
                compilationContext.RegisterSyntaxNodeAction(syntaxContext => AnalyzeConstructor(syntaxContext, candidates), SyntaxKind.ConstructorDeclaration);

                compilationContext.RegisterCompilationEndAction(compilationEndContext =>
                {
                    foreach ((Location location, string serviceClassName, string concreteTypeName) in candidates)
                    {
                        string interfaceName = InterfacePrefix + concreteTypeName;

                        if (!declaredInterfaceNames.ContainsKey(interfaceName))
                        {
                            continue;
                        }

                        var diagnostic = Diagnostic.Create(Rule, location, serviceClassName, concreteTypeName, interfaceName);
                        compilationEndContext.ReportDiagnostic(diagnostic);
                    }
                });
            });
        }

        private static void AnalyzeField(
            SyntaxNodeAnalysisContext context,
            ConcurrentBag<(Location Location, string ServiceClassName, string ConcreteTypeName)> candidates)
        {
            var field = (FieldDeclarationSyntax)context.Node;
            TypeDeclarationSyntax? containingType = CodingWellKnownTypes.GetContainingType(field);

            if (!IsServiceType(containingType))
            {
                return;
            }

            AnalyzeTypeSyntax(field.Declaration.Type, containingType!.Identifier.ValueText, candidates);
        }

        private static void AnalyzeProperty(
            SyntaxNodeAnalysisContext context,
            ConcurrentBag<(Location Location, string ServiceClassName, string ConcreteTypeName)> candidates)
        {
            var property = (PropertyDeclarationSyntax)context.Node;
            TypeDeclarationSyntax? containingType = CodingWellKnownTypes.GetContainingType(property);

            if (!IsServiceType(containingType))
            {
                return;
            }

            AnalyzeTypeSyntax(property.Type, containingType!.Identifier.ValueText, candidates);
        }

        private static void AnalyzeConstructor(
            SyntaxNodeAnalysisContext context,
            ConcurrentBag<(Location Location, string ServiceClassName, string ConcreteTypeName)> candidates)
        {
            var constructor = (ConstructorDeclarationSyntax)context.Node;
            TypeDeclarationSyntax? containingType = CodingWellKnownTypes.GetContainingType(constructor);

            if (!IsServiceType(containingType))
            {
                return;
            }

            foreach (ParameterSyntax parameter in constructor.ParameterList.Parameters)
            {
                if (parameter.Type is null)
                {
                    continue;
                }

                AnalyzeTypeSyntax(parameter.Type, containingType!.Identifier.ValueText, candidates);
            }
        }

        private static bool IsServiceType(TypeDeclarationSyntax? containingType)
        {
            return containingType is not null &&
                   containingType.Identifier.ValueText.EndsWith(ServiceSuffix, System.StringComparison.Ordinal);
        }

        private static void AnalyzeTypeSyntax(
            TypeSyntax typeSyntax,
            string serviceClassName,
            ConcurrentBag<(Location Location, string ServiceClassName, string ConcreteTypeName)> candidates)
        {
            string? typeName = typeSyntax switch
            {
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                GenericNameSyntax generic => generic.Identifier.ValueText,
                QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
                _ => null,
            };

            if (typeName is null ||
                !typeName.EndsWith(RepositorySuffix, System.StringComparison.Ordinal) ||
                IsInterfaceName(typeName))
            {
                return;
            }

            candidates.Add((typeSyntax.GetLocation(), serviceClassName, typeName));
        }

        private static bool IsInterfaceName(string typeName)
        {
            return typeName.Length > 1 &&
                   typeName[0] == InterfacePrefix[0] &&
                   char.IsUpper(typeName[1]);
        }
    }
}
