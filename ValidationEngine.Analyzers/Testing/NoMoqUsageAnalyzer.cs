using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Testing;

/// <summary>
/// TEST004: Flags usage of the Moq library (Mock&lt;T&gt; or the Moq namespace) in test code.
/// Per testing.12.3, NSubstitute is the approved mocking library and Moq must not be introduced.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoMoqUsageAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "TEST004";

    public static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Moq must not be used; NSubstitute is the approved mocking library",
        messageFormat: "'{0}' uses Moq, but NSubstitute is the approved mocking library",
        category: "Testing",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "NSubstitute is the approved mocking library. Moq must not be introduced. See testing.12.3.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeUsingDirective, SyntaxKind.UsingDirective);
        context.RegisterSyntaxNodeAction(AnalyzeGenericName, SyntaxKind.GenericName);
    }

    private static void AnalyzeUsingDirective(SyntaxNodeAnalysisContext context)
    {
        var usingDirective = (UsingDirectiveSyntax)context.Node;
        if (usingDirective.Name?.ToString() == TestingWellKnownTypes.MoqMockNamespace)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, usingDirective.GetLocation(), usingDirective.Name.ToString()));
        }
    }

    private static void AnalyzeGenericName(SyntaxNodeAnalysisContext context)
    {
        var genericName = (GenericNameSyntax)context.Node;
        if (genericName.Identifier.Text != "Mock")
        {
            return;
        }

        var symbol = context.SemanticModel.GetSymbolInfo(genericName, context.CancellationToken).Symbol
            ?? context.SemanticModel.GetTypeInfo(genericName, context.CancellationToken).Type;

        var containingNamespace = symbol?.ContainingNamespace?.ToDisplayString();
        if (containingNamespace == TestingWellKnownTypes.MoqMockNamespace)
        {
            SyntaxNode reportNode = genericName.Parent is QualifiedNameSyntax qualifiedName && qualifiedName.Right == genericName
                ? qualifiedName
                : genericName;

            context.ReportDiagnostic(Diagnostic.Create(Rule, reportNode.GetLocation(), reportNode.ToString()));
        }
    }
}
