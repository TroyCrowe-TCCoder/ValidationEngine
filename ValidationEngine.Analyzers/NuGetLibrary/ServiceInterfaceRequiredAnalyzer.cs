using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.NuGetLibrary;

/// <summary>
/// NUGET002: Flags public classes whose name ends with "Service" that do not implement a
/// corresponding public interface (e.g. "OrderService" must implement "IOrderService" or another
/// public interface). Per nuget-library.5.2, every public service exposed for dependency injection
/// must have a corresponding public interface.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ServiceInterfaceRequiredAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "NUGET002";

    public static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Public service type must implement a public interface",
        messageFormat: "Public service type '{0}' must implement a corresponding public interface for dependency injection",
        category: "NuGetLibrary",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Every public service exposed for dependency injection must have a corresponding public interface. See nuget-library.5.2.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
    }

    private static void AnalyzeType(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;

        if (type.TypeKind != TypeKind.Class
            || type.DeclaredAccessibility != Accessibility.Public
            || !type.Name.EndsWith(NuGetLibraryWellKnownTypes.ServiceNameSuffix, System.StringComparison.Ordinal))
        {
            return;
        }

        var implementsPublicInterface = type.AllInterfaces.Any(i => i.DeclaredAccessibility == Accessibility.Public);
        if (!implementsPublicInterface)
        {
            var location = type.Locations.FirstOrDefault();
            if (location is not null)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, location, type.Name));
            }
        }
    }
}
