using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Database;

/// <summary>
/// DB003: Flags positional (integer-literal) column access against a SqlDataReader/DbDataReader,
/// via either the indexer (reader[0]) or a Get* accessor (reader.GetInt32(0)). Per database.5.3,
/// GetOrdinal must be used to resolve column positions by name; positional index access is not permitted.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ReaderPositionalAccessAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "DB003";

    public static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Data reader columns must be resolved by name via GetOrdinal",
        messageFormat: "Use GetOrdinal(\"ColumnName\") to resolve the column position instead of a literal index",
        category: "Database",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "GetOrdinal must be used to resolve column positions by name; positional index access is not permitted. See database.5.3.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeElementAccess, SyntaxKind.ElementAccessExpression);
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeElementAccess(SyntaxNodeAnalysisContext context)
    {
        var elementAccess = (ElementAccessExpressionSyntax)context.Node;
        var arguments = elementAccess.ArgumentList.Arguments;
        if (arguments.Count != 1 || !IsIntegerLiteral(arguments[0].Expression))
        {
            return;
        }

        var semanticModel = context.SemanticModel;
        var typeInfo = semanticModel.GetTypeInfo(elementAccess.Expression, context.CancellationToken);
        if (!IsReaderType(typeInfo.Type))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, elementAccess.GetLocation()));
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess ||
            !memberAccess.Name.Identifier.Text.StartsWith("Get", System.StringComparison.Ordinal) ||
            memberAccess.Name.Identifier.Text == "GetOrdinal")
        {
            return;
        }

        var arguments = invocation.ArgumentList.Arguments;
        if (arguments.Count != 1 || !IsIntegerLiteral(arguments[0].Expression))
        {
            return;
        }

        var semanticModel = context.SemanticModel;
        var typeInfo = semanticModel.GetTypeInfo(memberAccess.Expression, context.CancellationToken);
        if (!IsReaderType(typeInfo.Type))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
    }

    private static bool IsIntegerLiteral(ExpressionSyntax expression)
        => expression is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NumericLiteralExpression };

    private static bool IsReaderType(ITypeSymbol? type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            var name = current.ToDisplayString();
            if (name == DatabaseWellKnownTypes.SqlDataReaderMetadataName ||
                name == DatabaseWellKnownTypes.MicrosoftSqlDataReaderMetadataName ||
                name == DatabaseWellKnownTypes.DbDataReaderMetadataName)
            {
                return true;
            }
        }

        return false;
    }
}
