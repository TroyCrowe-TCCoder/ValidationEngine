namespace ValidationEngine.Resolver;

public sealed record RepositoryProfile
{
    public required AppType AppType { get; init; }
    public required bool HasSpaClient { get; init; }
    public required bool SpaClientUsesReact { get; init; }
}
