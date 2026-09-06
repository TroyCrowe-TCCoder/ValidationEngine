using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Testing;

/// <summary>
/// TEST002: Flags test methods inside a "[TypeName]ObservabilityTests" class whose name does not
/// follow the [MethodOrOperation]When[Dependency]ThrowsThenRethrowsAndLogsError naming pattern.
/// Per testing.3.4, observability tests use a distinct naming category from ordinary behavior tests.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ObservabilityTestNamingAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "TEST002";

    private static readonly Regex ObservabilityNamePattern = new(
        @"^[A-Za-z0-9]+When[A-Za-z0-9]+Throws(Then)?Rethrows(And)?Logs?Error$",
        RegexOptions.Compiled);

    public static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Observability test method must follow the required naming pattern",
        messageFormat: "Observability test method '{0}' must follow the [MethodOrOperation]When[Dependency]ThrowsThenRethrowsAndLogsError naming pattern",
        category: "Testing",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Test classes named '[TypeName]ObservabilityTests' must name their test methods using the pattern [MethodOrOperation]When[Dependency]ThrowsThenRethrowsAndLogsError. See testing.3.4.");

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

        var classDeclaration = method.FirstAncestorOrSelf<ClassDeclarationSyntax>();
        if (classDeclaration is null || !classDeclaration.Identifier.Text.EndsWith("ObservabilityTests", System.StringComparison.Ordinal))
        {
            return;
        }

        if (!HasTestAttribute(method))
        {
            return;
        }

        var methodName = method.Identifier.Text;
        if (!ObservabilityNamePattern.IsMatch(methodName))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, method.Identifier.GetLocation(), methodName));
        }
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
