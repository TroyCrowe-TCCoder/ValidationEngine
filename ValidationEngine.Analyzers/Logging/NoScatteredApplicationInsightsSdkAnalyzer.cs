using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Logging
{
    /// <summary>
    /// Enforces GlobalLoggingStandards.md logging.2.3: Microsoft.ApplicationInsights.* types must not
    /// be referenced as instance state (fields/properties) scattered across arbitrary application
    /// classes. Application Insights wiring belongs in an infrastructure/options layer only.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class NoScatteredApplicationInsightsSdkAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "LOG003";

        private static readonly LocalizableString Title =
            "Application Insights SDK types must not be scattered across application classes";

        private static readonly LocalizableString MessageFormat =
            "'{0}' is referenced as state on '{1}'; confine Application Insights SDK types to an infrastructure/options layer, per GlobalLoggingStandards.md logging.2.3";

        private static readonly LocalizableString Description =
            "Direct references to Microsoft.ApplicationInsights.* types as fields or properties couple " +
            "application classes to a specific telemetry provider. Confine these types to an " +
            "infrastructure or options wiring layer. See GlobalLoggingStandards.md Section 2.3.";

        private const string Category = "Logging";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: "https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalLoggingStandards.md");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(compilationContext =>
            {
                compilationContext.RegisterSyntaxNodeAction(AnalyzeField, SyntaxKind.FieldDeclaration);
                compilationContext.RegisterSyntaxNodeAction(AnalyzeProperty, SyntaxKind.PropertyDeclaration);
            });
        }

        private static void AnalyzeField(SyntaxNodeAnalysisContext context)
        {
            var field = (FieldDeclarationSyntax)context.Node;
            AnalyzeTypeReference(context, field.Declaration.Type, LoggingWellKnownTypes.GetContainingType(field));
        }

        private static void AnalyzeProperty(SyntaxNodeAnalysisContext context)
        {
            var property = (PropertyDeclarationSyntax)context.Node;
            AnalyzeTypeReference(context, property.Type, LoggingWellKnownTypes.GetContainingType(property));
        }

        private static void AnalyzeTypeReference(
            SyntaxNodeAnalysisContext context,
            TypeSyntax typeSyntax,
            TypeDeclarationSyntax? containingType)
        {
            if (containingType is null)
            {
                return;
            }

            ISymbol? symbol = context.SemanticModel.GetSymbolInfo(typeSyntax, context.CancellationToken).Symbol;

            if (symbol is not ITypeSymbol typeSymbol)
            {
                return;
            }

            string? namespaceName = typeSymbol.ContainingNamespace?.ToDisplayString();

            if (namespaceName is null ||
                !namespaceName.StartsWith(LoggingWellKnownTypes.ApplicationInsightsNamespacePrefix, StringComparison.Ordinal))
            {
                return;
            }

            string containingTypeName = containingType.Identifier.ValueText;
            string containingNamespaceName = GetNamespaceName(containingType);

            if (LooksLikeInfrastructureOrOptions(containingTypeName) ||
                LooksLikeInfrastructureOrOptions(containingNamespaceName))
            {
                return;
            }

            var diagnostic = Diagnostic.Create(
                Rule,
                typeSyntax.GetLocation(),
                typeSymbol.Name,
                containingTypeName);

            context.ReportDiagnostic(diagnostic);
        }

        private static bool LooksLikeInfrastructureOrOptions(string name)
        {
            return name.IndexOf(LoggingWellKnownTypes.InfrastructureNamespaceFragment, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf(LoggingWellKnownTypes.OptionsNamespaceFragment, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string GetNamespaceName(SyntaxNode node)
        {
            SyntaxNode? current = node;

            while (current is not null)
            {
                switch (current)
                {
                    case BaseNamespaceDeclarationSyntax namespaceDeclaration:
                        return namespaceDeclaration.Name.ToString();
                }

                current = current.Parent;
            }

            return string.Empty;
        }
    }
}
