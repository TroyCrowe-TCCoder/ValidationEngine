# ValidationEngine.Models

Shared domain and reporting contracts for the [ValidationEngine](https://github.com/TroyCrowe-TCCoder/ValidationEngine)
standards validation suite.

## What's in this package

Thin, dependency-free data contracts consumed by the core engine, reporting plug-ins, the AI-assisted
Agent, and any custom consumer:

- `ValidationReport` — the top-level result of a validation run.
- `RuleFinding` — a single rule violation or manual-review item.
- `RunContext` — metadata describing the repository/run that produced a report.
- `EngineError` — a non-violation failure encountered by a validation tool.
- `RulePriority`, `ViolationSeverity`, `ValidationRunMode` — supporting enums.

This project intentionally has no dependency on `ValidationEngine`, `ValidationEngine.Reporting`, or
`ValidationEngine.Agent` — it exists so that neither producers (the core engine, the Agent) nor
consumers (reporting plug-ins, custom tooling) need to depend on each other directly.

## License

MIT — see [LICENSE](https://github.com/TroyCrowe-TCCoder/ValidationEngine/blob/main/LICENSE).
