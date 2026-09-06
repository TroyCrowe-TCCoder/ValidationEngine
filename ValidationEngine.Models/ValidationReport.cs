namespace ValidationEngine.Models;

public sealed record ValidationReport
{
    public required IReadOnlyList<RuleFinding> Findings { get; init; }
    public required IReadOnlyList<EngineError> EngineErrors { get; init; }
    public RunContext? RunContext { get; init; }

    public bool HasEngineErrors => EngineErrors.Count > 0;

    public bool HasViolations => Findings.Any(finding => finding.Severity == ViolationSeverity.Violation);
}
