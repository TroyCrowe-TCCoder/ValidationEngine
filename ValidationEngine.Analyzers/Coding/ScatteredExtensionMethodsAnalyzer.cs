using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Coding
{
    /// <summary>
    /// Enforces GlobalCodingStandards.md coding.3.13.1: each domain type gets exactly one extension
    /// class named after it, and all extension methods related to that domain type must live in
    /// that class. Flags extension methods for the same extended (domain) type declared across more
    /// than one static class.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ScatteredExtensionMethodsAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CODE012";

        private static readonly LocalizableString Title =
            "Extension methods for a domain type must be grouped in one class";

        private static readonly LocalizableString MessageFormat =
            "Extension method '{0}' extends '{1}', but extension methods for '{1}' are spread across multiple classes ('{2}'); consolidate them into a single '{1}Extensions' class, per GlobalCodingStandards.md coding.3.13.1";

        private static readonly LocalizableString Description =
            "Extension methods must be used whenever a transformation is consumed in more than one place. " +
            "Each domain type gets exactly one extension class named after it, and all extension methods " +
            "related to that domain type must live in that class. See GlobalCodingStandards.md Section 3.13.1.";

        private const string Category = "Coding";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: "https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalCodingStandards.md",
            customTags: WellKnownDiagnosticTags.CompilationEnd);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(compilationContext =>
            {
                var extensionMethods = new ConcurrentBag<(MethodDeclarationSyntax Method, string ExtendedTypeName, string ClassName)>();

                compilationContext.RegisterSyntaxNodeAction(syntaxContext =>
                {
                    var method = (MethodDeclarationSyntax)syntaxContext.Node;

                    if (!TryGetExtendedTypeName(method, out string? extendedTypeName))
                    {
                        return;
                    }

                    TypeDeclarationSyntax? containingType = CodingWellKnownTypes.GetContainingType(method);

                    if (containingType is null)
                    {
                        return;
                    }

                    extensionMethods.Add((method, extendedTypeName!, containingType.Identifier.ValueText));
                }, SyntaxKind.MethodDeclaration);

                compilationContext.RegisterCompilationEndAction(compilationEndContext =>
                {
                    var classesByExtendedType = new Dictionary<string, HashSet<string>>(System.StringComparer.Ordinal);

                    foreach ((MethodDeclarationSyntax _, string extendedTypeName, string className) in extensionMethods)
                    {
                        if (!classesByExtendedType.TryGetValue(extendedTypeName, out HashSet<string>? classNames))
                        {
                            classNames = new HashSet<string>(System.StringComparer.Ordinal);
                            classesByExtendedType[extendedTypeName] = classNames;
                        }

                        classNames.Add(className);
                    }

                    foreach ((MethodDeclarationSyntax method, string extendedTypeName, string className) in extensionMethods)
                    {
                        if (classesByExtendedType[extendedTypeName].Count <= 1)
                        {
                            continue;
                        }

                        string classList = string.Join(", ", SortedClassNames(classesByExtendedType[extendedTypeName]));

                        var diagnostic = Diagnostic.Create(
                            Rule,
                            method.Identifier.GetLocation(),
                            method.Identifier.ValueText,
                            extendedTypeName,
                            classList);

                        compilationEndContext.ReportDiagnostic(diagnostic);
                    }
                });
            });
        }

        private static List<string> SortedClassNames(HashSet<string> classNames)
        {
            var sorted = new List<string>(classNames);
            sorted.Sort(System.StringComparer.Ordinal);
            return sorted;
        }

        private static bool TryGetExtendedTypeName(MethodDeclarationSyntax method, out string? extendedTypeName)
        {
            extendedTypeName = null;

            if (!method.Modifiers.Any(SyntaxKind.StaticKeyword) || method.ParameterList.Parameters.Count == 0)
            {
                return false;
            }

            ParameterSyntax firstParameter = method.ParameterList.Parameters[0];

            if (!firstParameter.Modifiers.Any(SyntaxKind.ThisKeyword) || firstParameter.Type is null)
            {
                return false;
            }

            extendedTypeName = firstParameter.Type switch
            {
                GenericNameSyntax generic when generic.TypeArgumentList.Arguments.Count == 1 =>
                    GetSimpleTypeName(generic.TypeArgumentList.Arguments[0]),
                _ => GetSimpleTypeName(firstParameter.Type),
            };

            return extendedTypeName is not null;
        }

        private static string? GetSimpleTypeName(TypeSyntax typeSyntax)
        {
            return typeSyntax switch
            {
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                GenericNameSyntax generic => generic.Identifier.ValueText,
                QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
                _ => null,
            };
        }
    }
}
