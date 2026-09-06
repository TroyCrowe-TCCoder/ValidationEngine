using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.NuGetLibrary;

/// <summary>
/// NUGET003: Flags additional public static classes named "ServiceCollectionExtensions" when more
/// than one such class exists in the same compilation. Per nuget-library.5.3, a shared library must
/// provide a single ServiceCollectionExtensions entry point for registration.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SingleServiceCollectionExtensionsAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "NUGET003";

    public static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Only one ServiceCollectionExtensions class is allowed per library",
        messageFormat: "Library must provide a single 'ServiceCollectionExtensions' registration entry point; found additional class '{0}'",
        category: "NuGetLibrary",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A shared library that requires service registration must provide a single ServiceCollectionExtensions entry point. See nuget-library.5.3.",
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    private static void AnalyzeCompilation(CompilationAnalysisContext context)
    {
        var classDeclarations = context.Compilation.SyntaxTrees
            .SelectMany(tree => tree.GetRoot(context.CancellationToken).DescendantNodes().OfType<ClassDeclarationSyntax>())
            .Where(c => c.Identifier.Text == NuGetLibraryWellKnownTypes.ServiceCollectionExtensionsClassName)
            .ToList();

        if (classDeclarations.Count <= 1)
        {
            return;
        }

        foreach (var extra in classDeclarations.Skip(1))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, extra.Identifier.GetLocation(), extra.Identifier.Text));
        }
    }
}
