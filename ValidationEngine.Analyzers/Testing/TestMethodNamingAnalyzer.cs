using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Testing;

/// <summary>
/// TEST001: Flags [Fact]/[Theory] test methods whose name does not follow the
/// [MethodUnderTest]_[Scenario]_[ExpectedResult] naming pattern (at least two underscore-separated
/// segments). Per testing.3.2, test method names must communicate the full purpose of the test.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TestMethodNamingAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "TEST001";

    public static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Test method must follow [MethodUnderTest]_[Scenario]_[ExpectedResult] naming",
        messageFormat: "Test method '{0}' must follow the [MethodUnderTest]_[Scenario]_[ExpectedResult] naming pattern",
        category: "Testing",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Test method names must identify the method under test, the scenario, and the expected result, separated by underscores. See testing.3.2.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var method = (MethodDeclarationSyntax)context.Node;

        if (!HasTestAttribute(method))
        {
            return;
        }

        var methodName = method.Identifier.Text;

        // Observability and guard-clause test naming are distinct categories handled by their own
        // conventions (testing.3.4, testing.3.5) and must not be double-flagged here.
        if (IsObservabilityTestClass(method))
        {
            return;
        }

        var segments = methodName.Split('_');
        var nonEmptySegments = segments.Count(s => s.Length > 0);

        if (segments.Length < 3 || nonEmptySegments < 3)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, method.Identifier.GetLocation(), methodName));
        }
    }

    private static bool IsObservabilityTestClass(MethodDeclarationSyntax method)
    {
        var classDeclaration = method.FirstAncestorOrSelf<ClassDeclarationSyntax>();
        return classDeclaration is not null && classDeclaration.Identifier.Text.EndsWith("ObservabilityTests", System.StringComparison.Ordinal);
    }

    private static bool HasTestAttribute(MethodDeclarationSyntax method)
    {
        foreach (var attributeList in method.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var name = GetSimpleAttributeName(attribute.Name);
                if (name is "Fact" or "FactAttribute" or "Theory" or "TheoryAttribute")
                {
                    return true;
                }
            }
        }

        return false;
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
