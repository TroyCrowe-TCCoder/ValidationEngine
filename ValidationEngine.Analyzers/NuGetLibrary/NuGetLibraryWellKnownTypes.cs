namespace ValidationEngine.Analyzers.NuGetLibrary;

/// <summary>
/// Well-known type/name fragments used across the NuGetLibrary analyzers.
/// </summary>
internal static class NuGetLibraryWellKnownTypes
{
    public const string ServiceCollectionExtensionsClassName = "ServiceCollectionExtensions";
    public const string ServiceCollectionInterfaceMetadataName = "Microsoft.Extensions.DependencyInjection.IServiceCollection";
    public const string OptionsGenericInterfaceMetadataName = "Microsoft.Extensions.Options.IOptions`1";
    public const string ServiceNameSuffix = "Service";
    public const string OptionsNameSuffix = "Options";
}
