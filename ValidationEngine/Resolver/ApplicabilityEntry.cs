namespace ValidationEngine.Resolver;

public sealed record ApplicabilityEntry
{
    public required string StandardFile { get; init; }
    public required string MarkerBase { get; init; }
    public required ApplicabilityStatus WebAppWebApi { get; init; }
    public required ApplicabilityStatus Database { get; init; }
    public required ApplicabilityStatus ClassLibrary { get; init; }
}
