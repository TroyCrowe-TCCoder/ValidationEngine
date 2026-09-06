using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Coding
{
    /// <summary>
    /// Enforces GlobalCodingStandards.md coding.3.5: data transfer objects that are not modified
    /// after construction must be implemented as immutable types using <c>init</c>-only properties
    /// or C# records. Flags DTO-suffixed/Model-suffixed classes with mutable (<c>set</c>)
    /// properties.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MutableDtoAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CODE009";

        private static readonly LocalizableString Title =
            "DTOs and models must be immutable";

        private static readonly LocalizableString MessageFormat =
            "Property '{0}' on '{1}' has a mutable 'set' accessor; use 'init' or a record so the type is immutable after construction, per GlobalCodingStandards.md coding.3.5";

        private static readonly LocalizableString Description =
            "Data transfer objects that are not modified after construction must be implemented as immutable " +
            "types using init-only properties or C# records. See GlobalCodingStandards.md Section 3.5.";

        private const string Category = "Coding";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: "https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalCodingStandards.md");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(compilationContext =>
            {
                compilationContext.RegisterSyntaxNodeAction(AnalyzeClass, SyntaxKind.ClassDeclaration);
            });
        }

        private static void AnalyzeClass(SyntaxNodeAnalysisContext context)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;
            string className = classDeclaration.Identifier.ValueText;

            if (!CodingWellKnownTypes.IsDtoOrModelTypeName(className))
            {
                return;
            }

            foreach (MemberDeclarationSyntax member in classDeclaration.Members)
            {
                if (member is not PropertyDeclarationSyntax property || property.AccessorList is null)
                {
                    continue;
                }

                foreach (AccessorDeclarationSyntax accessor in property.AccessorList.Accessors)
                {
                    if (!accessor.IsKind(SyntaxKind.SetAccessorDeclaration))
                    {
                        continue;
                    }

                    var diagnostic = Diagnostic.Create(
                        Rule,
                        accessor.GetLocation(),
                        property.Identifier.ValueText,
                        className);

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
