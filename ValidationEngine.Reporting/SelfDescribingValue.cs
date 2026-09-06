namespace ValidationEngine.Reporting;

/// <summary>
/// Wraps a value alongside the full set of options it was chosen from, so JSON consumers can
/// see all possible values (e.g., all <c>ValidationRunMode</c> members) without a separate schema.
/// </summary>
internal sealed record SelfDescribingValue<T>
{
    public IReadOnlyList<T> Options { get; }
    public T Value { get; }

    public SelfDescribingValue(IReadOnlyList<T> options, T value)
    {
        Options = options;
        Value = value;
    }
}
