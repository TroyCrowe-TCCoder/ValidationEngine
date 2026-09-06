using ValidationEngine.Infrastructure;
using ValidationEngine.Models;

namespace ValidationEngine.Reporting;

/// <summary>
/// JSON shape for <see cref="RunContext"/> within a <see cref="ValidationReportDocument"/>.
/// </summary>
internal sealed record RunContextDto
{
    public string RepositoryName { get; }
    public string RepositoryRoot { get; }
    public SelfDescribingValue<ValidationRunMode> Mode { get; }
    public string? TargetBranch { get; }
    public string DetectedAppType { get; }
    public IReadOnlyList<string> ApplicableStandards { get; }
    public int ChangesEvaluated { get; }

    public RunContextDto(RunContext runContext)
    {
        RepositoryName = runContext.RepositoryName;
        RepositoryRoot = runContext.RepositoryRoot;
        Mode = new SelfDescribingValue<ValidationRunMode>(Enum.GetValues<ValidationRunMode>(), runContext.Mode);
        TargetBranch = runContext.TargetBranch;
        DetectedAppType = runContext.DetectedAppType;
        ApplicableStandards = runContext.ApplicableStandards;
        ChangesEvaluated = runContext.ChangesEvaluated;
    }
}
