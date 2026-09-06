using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Coding
{
    /// <summary>
    /// Enforces GlobalCodingStandards.md coding.3.7: every member must have the minimum visibility
    /// required. Flags methods, properties, and fields declared with an explicit <c>public</c>
    /// modifier inside a type that is itself explicitly declared <c>internal</c> - the member's
    /// effective reachable visibility is already capped by the containing type, so the wider
    /// modifier serves no purpose unless the member implements an interface member.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class NonMinimalVisibilityAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CODE011";

        private static readonly LocalizableString Title =
            "Members must have the minimum visibility required";

        private static readonly LocalizableString MessageFormat =
            "Member '{0}' is declared 'public' inside internal type '{1}'; narrow its visibility since the containing type already limits reachability outside the assembly, per GlobalCodingStandards.md coding.3.7";

        private static readonly LocalizableString Description =
            "Every member must have the minimum visibility required. Methods, properties, and fields must be " +
            "private unless a concrete, present reason requires wider visibility. Visibility must not be widened " +
            "without that reason being identifiable. See GlobalCodingStandards.md Section 3.7.";

        private const string Category = "Coding";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
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
                compilationContext.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
                compilationContext.RegisterSyntaxNodeAction(AnalyzeProperty, SyntaxKind.PropertyDeclaration);
                compilationContext.RegisterSyntaxNodeAction(AnalyzeField, SyntaxKind.FieldDeclaration);
            });
        }

        private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            var method = (MethodDeclarationSyntax)context.Node;

            if (!TryGetInternalContainingType(method, out TypeDeclarationSyntax? containingType) ||
                !HasExplicitPublicModifier(method.Modifiers))
            {
                return;
            }

            if (context.SemanticModel.GetDeclaredSymbol(method, context.CancellationToken) is not IMethodSymbol symbol ||
                symbol.IsOverride ||
                ImplementsInterfaceMember(symbol))
            {
                return;
            }

            Report(context, method.Identifier, method.Identifier.ValueText, containingType!.Identifier.ValueText);
        }

        private static void AnalyzeProperty(SyntaxNodeAnalysisContext context)
        {
            var property = (PropertyDeclarationSyntax)context.Node;

            if (!TryGetInternalContainingType(property, out TypeDeclarationSyntax? containingType) ||
                !HasExplicitPublicModifier(property.Modifiers))
            {
                return;
            }

            if (context.SemanticModel.GetDeclaredSymbol(property, context.CancellationToken) is not IPropertySymbol symbol ||
                symbol.IsOverride ||
                ImplementsInterfaceMember(symbol))
            {
                return;
            }

            Report(context, property.Identifier, property.Identifier.ValueText, containingType!.Identifier.ValueText);
        }

        private static void AnalyzeField(SyntaxNodeAnalysisContext context)
        {
            var field = (FieldDeclarationSyntax)context.Node;

            if (!TryGetInternalContainingType(field, out TypeDeclarationSyntax? containingType) ||
                !HasExplicitPublicModifier(field.Modifiers))
            {
                return;
            }

            foreach (VariableDeclaratorSyntax declarator in field.Declaration.Variables)
            {
                Report(context, declarator.Identifier, declarator.Identifier.ValueText, containingType!.Identifier.ValueText);
            }
        }

        private static void Report(SyntaxNodeAnalysisContext context, SyntaxToken identifier, string memberName, string typeName)
        {
            var diagnostic = Diagnostic.Create(Rule, identifier.GetLocation(), memberName, typeName);
            context.ReportDiagnostic(diagnostic);
        }

        private static bool TryGetInternalContainingType(SyntaxNode member, out TypeDeclarationSyntax? containingType)
        {
            containingType = CodingWellKnownTypes.GetContainingType(member);

            return containingType is not null &&
                   containingType is not InterfaceDeclarationSyntax &&
                   HasExplicitInternalModifier(containingType.Modifiers);
        }

        private static bool HasExplicitInternalModifier(SyntaxTokenList modifiers)
        {
            foreach (SyntaxToken modifier in modifiers)
            {
                if (modifier.IsKind(SyntaxKind.InternalKeyword))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasExplicitPublicModifier(SyntaxTokenList modifiers)
        {
            foreach (SyntaxToken modifier in modifiers)
            {
                if (modifier.IsKind(SyntaxKind.PublicKeyword))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ImplementsInterfaceMember(ISymbol symbol)
        {
            INamedTypeSymbol? containingType = symbol.ContainingType;

            if (containingType is null)
            {
                return false;
            }

            foreach (INamedTypeSymbol interfaceType in containingType.AllInterfaces)
            {
                foreach (ISymbol interfaceMember in interfaceType.GetMembers())
                {
                    ISymbol? implementation = containingType.FindImplementationForInterfaceMember(interfaceMember);

                    if (implementation is not null && SymbolEqualityComparer.Default.Equals(implementation, symbol))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
