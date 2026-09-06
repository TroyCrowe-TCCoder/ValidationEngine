using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Database;

/// <summary>
/// DB001: Flags SqlCommand.CommandType set to CommandType.Text (or left unset while a raw SQL
/// string is assigned to CommandText). Per database.5.2, CommandType must always be
/// CommandType.StoredProcedure and must never be CommandType.Text.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CommandTypeMustBeStoredProcedureAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "DB001";

    public static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "CommandType must be StoredProcedure",
        messageFormat: "SqlCommand.CommandType must be set to CommandType.StoredProcedure, not CommandType.Text",
        category: "Database",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "All database operations must be executed through stored procedures. CommandType.Text (inline SQL) is prohibited. See database.5.2.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeAssignment, SyntaxKind.SimpleAssignmentExpression);
    }

    private static void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
    {
        var assignment = (AssignmentExpressionSyntax)context.Node;

        if (assignment.Left is not MemberAccessExpressionSyntax memberAccess ||
            memberAccess.Name.Identifier.Text != DatabaseWellKnownTypes.CommandTypePropertyName)
        {
            return;
        }

        var semanticModel = context.SemanticModel;
        var leftSymbol = semanticModel.GetSymbolInfo(memberAccess, context.CancellationToken).Symbol;
        if (leftSymbol is not IPropertySymbol { } propertySymbol ||
            !IsSqlCommandType(propertySymbol.ContainingType))
        {
            return;
        }

        if (assignment.Right is not MemberAccessExpressionSyntax rightAccess ||
            rightAccess.Name.Identifier.Text != DatabaseWellKnownTypes.CommandTypeTextMemberName)
        {
            return;
        }

        var rightSymbol = semanticModel.GetSymbolInfo(rightAccess, context.CancellationToken).Symbol;
        if (rightSymbol is IFieldSymbol { ContainingType: { } enumType } &&
            enumType.ToDisplayString() == DatabaseWellKnownTypes.CommandTypeEnumMetadataName)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, assignment.GetLocation()));
        }
    }

    private static bool IsSqlCommandType(ITypeSymbol? type)
    {
        if (type is null)
        {
            return false;
        }

        var name = type.ToDisplayString();
        return name == DatabaseWellKnownTypes.SqlCommandMetadataName ||
               name == DatabaseWellKnownTypes.MicrosoftSqlCommandMetadataName;
    }
}
