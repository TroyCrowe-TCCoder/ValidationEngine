using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Testing;

/// <summary>
/// TEST003: Flags raw System.Diagnostics.Stopwatch usage inside a test class that has no
/// [Benchmark]-attributed methods (i.e. is not a BenchmarkDotNet benchmark class). Per testing.11,
/// performance tests must use BenchmarkDotNet rather than a raw Stopwatch in a single-run test.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoRawStopwatchInPerformanceTestAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "TEST003";

    public static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Performance tests must use BenchmarkDotNet, not a raw Stopwatch",
        messageFormat: "Test class '{0}' must not use System.Diagnostics.Stopwatch directly; use a BenchmarkDotNet [Benchmark] method instead",
        category: "Testing",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A raw Stopwatch in a single-run test is affected by JIT warm-up, GC pressure, and OS scheduling. Performance tests must be written using BenchmarkDotNet. See testing.11.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
    }

    private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        var creation = (ObjectCreationExpressionSyntax)context.Node;

        var typeSymbol = context.SemanticModel.GetTypeInfo(creation, context.CancellationToken).Type;
        if (typeSymbol is null || typeSymbol.ToDisplayString() != TestingWellKnownTypes.StopwatchMetadataName)
        {
            return;
        }

        var classDeclaration = creation.FirstAncestorOrSelf<ClassDeclarationSyntax>();
        if (classDeclaration is null || !classDeclaration.Identifier.Text.EndsWith("Tests", System.StringComparison.Ordinal))
        {
            return;
        }

        if (HasBenchmarkMethod(classDeclaration))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, creation.GetLocation(), classDeclaration.Identifier.Text));
    }

    private static bool HasBenchmarkMethod(ClassDeclarationSyntax classDeclaration)
    {
        return classDeclaration.Members
            .OfType<MethodDeclarationSyntax>()
            .Any(m => m.AttributeLists
                .SelectMany(a => a.Attributes)
                .Any(attr => GetSimpleAttributeName(attr.Name) is "Benchmark" or "BenchmarkAttribute"));
    }

    private static string GetSimpleAttributeName(NameSyntax name)
    {
        return name switch
        {
            QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
            SimpleNameSyntax simple => simple.Identifier.Text,
            _ => name.ToString(),
        };
    }
}
