using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Caching
{
    /// <summary>
    /// Enforces GlobalCachingStandards.md caching.6.4: cache expiration values must be defined in
    /// application configuration and bound to a strongly typed "*CacheOptions" options class. No
    /// default values may be assigned on the class's properties (all values must come from
    /// configuration), and every such class must be registered via
    /// <c>Configure&lt;T&gt;(...)</c>.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CacheOptionsConfigurationAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CACHE007";

        private const string ConfigureMethodName = "Configure";

        private static readonly LocalizableString Title =
            "CacheOptions classes must have no default property values and must be registered via Configure<T>";

        private static readonly LocalizableString MessageFormat = "{0}";

        private static readonly LocalizableString Description =
            "All cache expiration values must be defined in application configuration and bound to a " +
            "strongly typed *CacheOptions options class. No default values may be assigned on the " +
            "class's properties, and the class must be registered via Configure<T>(...). See " +
            "GlobalCachingStandards.md Section 6.4.";

        private const string Category = "Caching";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: "https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalCachingStandards.md",
            customTags: WellKnownDiagnosticTags.CompilationEnd);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(compilationContext =>
            {
                var registeredOptionsTypeNames = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);
                var cacheOptionsClasses = new ConcurrentBag<ClassDeclarationSyntax>();

                compilationContext.RegisterSyntaxNodeAction(
                    syntaxContext => AnalyzeClassDeclaration(syntaxContext, cacheOptionsClasses),
                    SyntaxKind.ClassDeclaration);

                compilationContext.RegisterSyntaxNodeAction(
                    syntaxContext => CollectConfigureRegistration(syntaxContext, registeredOptionsTypeNames),
                    SyntaxKind.InvocationExpression);

                compilationContext.RegisterCompilationEndAction(
                    endContext => ReportMissingRegistrations(endContext, cacheOptionsClasses, registeredOptionsTypeNames));
            });
        }

        private static void AnalyzeClassDeclaration(
            SyntaxNodeAnalysisContext context,
            ConcurrentBag<ClassDeclarationSyntax> cacheOptionsClasses)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;
            string className = classDeclaration.Identifier.ValueText;

            if (!className.EndsWith(CachingWellKnownTypes.CacheOptionsClassNameSuffix, StringComparison.Ordinal))
            {
                return;
            }

            cacheOptionsClasses.Add(classDeclaration);

            foreach (PropertyDeclarationSyntax property in classDeclaration.Members.OfType<PropertyDeclarationSyntax>())
            {
                if (property.Initializer is null)
                {
                    continue;
                }

                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    property.Initializer.GetLocation(),
                    $"'{className}.{property.Identifier.ValueText}' assigns a default value; all cache expiration values must come from configuration, not a default on the options class, per GlobalCachingStandards.md caching.6.4"));
            }
        }

        private static void CollectConfigureRegistration(
            SyntaxNodeAnalysisContext context,
            ConcurrentDictionary<string, byte> registeredOptionsTypeNames)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            GenericNameSyntax? genericName = invocation.Expression switch
            {
                MemberAccessExpressionSyntax { Name: GenericNameSyntax generic } => generic,
                GenericNameSyntax generic => generic,
                _ => null,
            };

            if (genericName is null ||
                genericName.Identifier.ValueText != ConfigureMethodName ||
                genericName.TypeArgumentList.Arguments.Count != 1)
            {
                return;
            }

            string typeArgumentName = genericName.TypeArgumentList.Arguments[0] switch
            {
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
                _ => string.Empty,
            };

            if (typeArgumentName.Length > 0)
            {
                registeredOptionsTypeNames.TryAdd(typeArgumentName, 0);
            }
        }

        private static void ReportMissingRegistrations(
            CompilationAnalysisContext context,
            ConcurrentBag<ClassDeclarationSyntax> cacheOptionsClasses,
            ConcurrentDictionary<string, byte> registeredOptionsTypeNames)
        {
            var reportedClassNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (ClassDeclarationSyntax classDeclaration in cacheOptionsClasses)
            {
                string className = classDeclaration.Identifier.ValueText;

                if (registeredOptionsTypeNames.ContainsKey(className) || !reportedClassNames.Add(className))
                {
                    continue;
                }

                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    classDeclaration.Identifier.GetLocation(),
                    $"'{className}' is never registered via Configure<{className}>(...); strongly typed CacheOptions classes must be bound to configuration, per GlobalCachingStandards.md caching.6.4"));
            }
        }
    }
}
