# ValidationEngine.Reporting

Default Markdown and JSON report renderer plug-ins for the
[ValidationEngine](https://github.com/TroyCrowe-TCCoder/ValidationEngine) standards validation suite.

## What It Is

A NuGet library providing the default, optional renderer implementations for a
`ValidationEngine.Models.ValidationReport`. It is a plug-in library, not a runnable tool.

## What It Does

- `MarkdownReportRenderer` — renders a `ValidationEngine.Models.ValidationReport` as human-readable
  Markdown (manual review items, blocking violations, engine errors).
- `JsonReportRenderer` — renders the same report as machine-readable JSON with camel-cased enums,
  suitable for CI pipelines and tooling integration.

## Its Modularity

Reporting is a plug-in of the core engine, not a required dependency. Any consumer of
`ValidationEngine.Models` is free to implement its own `IMarkdownReportRenderer`/`IJsonReportRenderer`
instead of referencing this package — the core engine and the Agent never assume this project is the
reporting source, they only depend on the shared contracts in `ValidationEngine.Models`.

## How It Works

Either renderer takes a `ValidationReport` (produced by the core engine or the Agent) and converts
it to its target format. `validation-engine` and `validation-engine-agent` reference this package
as a composition-root convenience so they have working output by default, but neither depends on
it beyond that — either can be swapped for a custom renderer without any change to the producers.

## Installing

```bash
dotnet add package ValidationEngine.Reporting
```

## License

MIT — see [LICENSE](https://github.com/TroyCrowe-TCCoder/ValidationEngine/blob/main/LICENSE).
