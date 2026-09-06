using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.NuGetLibrary;

/// <summary>
/// NUGET001: Flags public, non-static, non-abstract, non-record class declarations that are not
/// marked sealed. Per nuget-library.5.1, public types must be sealed unless inheritance is part of
/// the intended library contract (abstract types are excluded).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SealedPublicTypeAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "NUGET001";

    public static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Public library type must be sealed",
        messageFormat: "Public type '{0}' must be marked sealed unless inheritance is part of the intended library contract",
        category: "NuGetLibrary",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Public types in a shared library must be sealed unless inheritance is part of the intended library contract. See nuget-library.5.1.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeClass, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeClass(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        if (!classDeclaration.Modifiers.Any(SyntaxKind.PublicKeyword))
        {
            return;
        }

        if (classDeclaration.Modifiers.Any(SyntaxKind.SealedKeyword)
            || classDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword)
            || classDeclaration.Modifiers.Any(SyntaxKind.AbstractKeyword)
            || classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, classDeclaration.Identifier.GetLocation(), classDeclaration.Identifier.Text));
    }
}
