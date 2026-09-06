using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Coding
{
    /// <summary>
    /// Enforces GlobalCodingStandards.md coding.2.5: an interface must not be created solely to
    /// wrap a single concrete class that is never substituted. Flags interfaces declared in the
    /// compilation that have exactly one implementing class and are not used as a generic type
    /// constraint (which would indicate substitution/testability usage).
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class WrapperOnlyInterfaceAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CODE003";

        private static readonly LocalizableString Title =
            "Interface must not solely wrap a single never-substituted class";

        private static readonly LocalizableString MessageFormat =
            "Interface '{0}' has exactly one implementation ('{1}') and is never used as a type-parameter constraint; verify it is not a wrapper-only interface, per GlobalCodingStandards.md coding.2.5";

        private static readonly LocalizableString Description =
            "Interfaces must be kept small and purposeful. An interface must not be created solely to wrap a " +
            "single concrete class that is never substituted. See GlobalCodingStandards.md Section 2.5.";

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
                var declaredInterfaces = new ConcurrentBag<(INamedTypeSymbol Symbol, Location Location)>();
                var implementationCounts = new ConcurrentDictionary<INamedTypeSymbol, int>(SymbolEqualityComparer.Default);
                var implementationNames = new ConcurrentDictionary<INamedTypeSymbol, string>(SymbolEqualityComparer.Default);
                var constraintUsedInterfaces = new ConcurrentDictionary<INamedTypeSymbol, byte>(SymbolEqualityComparer.Default);

                compilationContext.RegisterSyntaxNodeAction(syntaxContext =>
                {
                    var interfaceDeclaration = (InterfaceDeclarationSyntax)syntaxContext.Node;

                    if (syntaxContext.SemanticModel.GetDeclaredSymbol(interfaceDeclaration, syntaxContext.CancellationToken)
                        is not INamedTypeSymbol interfaceSymbol)
                    {
                        return;
                    }

                    declaredInterfaces.Add((interfaceSymbol, interfaceDeclaration.Identifier.GetLocation()));
                }, SyntaxKind.InterfaceDeclaration);

                compilationContext.RegisterSyntaxNodeAction(syntaxContext =>
                {
                    var typeDeclaration = (TypeDeclarationSyntax)syntaxContext.Node;

                    if (typeDeclaration is InterfaceDeclarationSyntax)
                    {
                        return;
                    }

                    if (syntaxContext.SemanticModel.GetDeclaredSymbol(typeDeclaration, syntaxContext.CancellationToken)
                        is not INamedTypeSymbol typeSymbol)
                    {
                        return;
                    }

                    foreach (INamedTypeSymbol implementedInterface in typeSymbol.AllInterfaces)
                    {
                        implementationCounts.AddOrUpdate(implementedInterface, 1, (_, count) => count + 1);
                        implementationNames[implementedInterface] = typeSymbol.Name;
                    }
                },
                SyntaxKind.ClassDeclaration,
                SyntaxKind.RecordDeclaration,
                SyntaxKind.StructDeclaration);

                compilationContext.RegisterSyntaxNodeAction(syntaxContext =>
                {
                    var typeParameterConstraint = (TypeParameterConstraintClauseSyntax)syntaxContext.Node;

                    foreach (TypeParameterConstraintSyntax constraint in typeParameterConstraint.Constraints)
                    {
                        if (constraint is not TypeConstraintSyntax typeConstraint)
                        {
                            continue;
                        }

                        if (syntaxContext.SemanticModel.GetSymbolInfo(typeConstraint.Type, syntaxContext.CancellationToken).Symbol
                            is INamedTypeSymbol { TypeKind: TypeKind.Interface } constraintInterface)
                        {
                            constraintUsedInterfaces.TryAdd(constraintInterface, 0);
                        }
                    }
                }, SyntaxKind.TypeParameterConstraintClause);

                compilationContext.RegisterCompilationEndAction(compilationEndContext =>
                {
                    foreach ((INamedTypeSymbol interfaceSymbol, Location location) in declaredInterfaces)
                    {
                        if (!implementationCounts.TryGetValue(interfaceSymbol, out int count) || count != 1)
                        {
                            continue;
                        }

                        if (constraintUsedInterfaces.ContainsKey(interfaceSymbol))
                        {
                            continue;
                        }

                        string implementationName = implementationNames[interfaceSymbol];

                        var diagnostic = Diagnostic.Create(
                            Rule,
                            location,
                            interfaceSymbol.Name,
                            implementationName);

                        compilationEndContext.ReportDiagnostic(diagnostic);
                    }
                });
            });
        }
    }
}
