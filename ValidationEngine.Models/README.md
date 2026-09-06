# ValidationEngine.Models

Shared domain and reporting contracts for the [ValidationEngine](https://github.com/TroyCrowe-TCCoder/ValidationEngine)
standards validation suite.

## What It Is

A thin, dependency-free NuGet library of data contracts — the common vocabulary shared by every
other component in the suite. It is a library, not a runnable tool.

## What It Does

Defines the shared domain and reporting contracts consumed by the core engine, reporting
plug-ins, the AI-assisted Agent, and any custom consumer:

- `ValidationReport` — the top-level result of a validation run.
- `RuleFinding` — a single rule violation or manual-review item.
- `RunContext` — metadata describing the repository/run that produced a report.
- `EngineError` — a non-violation failure encountered by a validation tool.
- `RulePriority`, `ViolationSeverity`, `ValidationRunMode` — supporting enums.

## Its Modularity

This project intentionally has no dependency on `ValidationEngine`, `ValidationEngine.Reporting`, or
`ValidationEngine.Agent` — it exists so that neither producers (the core engine, the Agent) nor
consumers (reporting plug-ins, custom tooling) need to depend on each other directly. Any component
that needs to produce or consume a `ValidationReport` only needs to reference this package.

## How It Works

Producers (the core engine, the Agent) populate a `ValidationReport` with `RuleFinding` and
`EngineError` entries as they evaluate rules. Consumers (renderer plug-ins such as
`ValidationEngine.Reporting`, or custom tooling) read that same report to produce output, without
any of these components needing to know about each other's implementation.

## Installing

```bash
dotnet add package ValidationEngine.Models
```

## License

MIT — see [LICENSE](https://github.com/TroyCrowe-TCCoder/ValidationEngine/blob/main/LICENSE).
