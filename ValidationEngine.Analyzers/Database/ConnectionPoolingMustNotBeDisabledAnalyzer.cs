using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Database;

/// <summary>
/// DB004: Flags connection string literals that contain "Pooling=false". Per database.8.3, ADO.NET
/// connection pooling is enabled by default and must not be disabled.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConnectionPoolingMustNotBeDisabledAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "DB004";

    public static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Connection pooling must not be disabled",
        messageFormat: "Connection string must not include 'Pooling=false'; ADO.NET connection pooling is enabled by default and must remain enabled",
        category: "Database",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "ADO.NET connection pooling is enabled by default and must not be disabled. Connection strings must not include Pooling=false. See database.8.3.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeStringLiteral, SyntaxKind.StringLiteralExpression);
        context.RegisterSyntaxNodeAction(AnalyzeInterpolatedString, SyntaxKind.InterpolatedStringExpression);
    }

    private static void AnalyzeStringLiteral(SyntaxNodeAnalysisContext context)
    {
        var literal = (LiteralExpressionSyntax)context.Node;
        var text = literal.Token.ValueText;
        if (ContainsPoolingDisabled(text))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, literal.GetLocation()));
        }
    }

    private static void AnalyzeInterpolatedString(SyntaxNodeAnalysisContext context)
    {
        var interpolated = (InterpolatedStringExpressionSyntax)context.Node;
        var text = interpolated.ToString();
        if (ContainsPoolingDisabled(text))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, interpolated.GetLocation()));
        }
    }

    private static bool ContainsPoolingDisabled(string text)
        => text.Replace(" ", string.Empty)
               .IndexOf(DatabaseWellKnownTypes.ConnectionStringPoolingSegment, System.StringComparison.OrdinalIgnoreCase) >= 0;
}
