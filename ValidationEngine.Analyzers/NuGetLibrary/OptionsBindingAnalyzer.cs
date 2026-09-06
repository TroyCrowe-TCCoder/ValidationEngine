using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.NuGetLibrary;

/// <summary>
/// NUGET004: Flags public classes whose name ends with "Options" that do not expose only simple
/// property members (i.e. contain methods with a body), since options types must be plain
/// configuration objects bound through the standard options pattern. Per nuget-library.5.4, a
/// configurable shared library must expose a dedicated options type bound through the standard
/// options pattern.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OptionsBindingAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "NUGET004";

    public static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Options type must be a plain configuration object",
        messageFormat: "Options type '{0}' must expose only properties; it must not declare methods with logic",
        category: "NuGetLibrary",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A configurable shared library must expose a dedicated options type bound through the standard options pattern. See nuget-library.5.4.");

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

        if (!classDeclaration.Modifiers.Any(SyntaxKind.PublicKeyword)
            || !classDeclaration.Identifier.Text.EndsWith(NuGetLibraryWellKnownTypes.OptionsNameSuffix, System.StringComparison.Ordinal))
        {
            return;
        }

        var hasMethodWithBody = classDeclaration.Members
            .OfType<MethodDeclarationSyntax>()
            .Any(m => m.Body is not null || m.ExpressionBody is not null);

        if (hasMethodWithBody)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, classDeclaration.Identifier.GetLocation(), classDeclaration.Identifier.Text));
        }
    }
}
