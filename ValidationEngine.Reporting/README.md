# ValidationEngine.Reporting

Default Markdown and JSON report renderer plug-ins for the
[ValidationEngine](https://github.com/TroyCrowe-TCCoder/ValidationEngine) standards validation suite.

## What's in this package

- `MarkdownReportRenderer` — renders a `ValidationEngine.Models.ValidationReport` as human-readable
  Markdown (manual review items, blocking violations, engine errors).
- `JsonReportRenderer` — renders the same report as machine-readable JSON with camel-cased enums,
  suitable for CI pipelines and tooling integration.

## Plug-in, not a dependency

Reporting is a plug-in of the core engine, not a required dependency. Any consumer of
`ValidationEngine.Models` is free to implement its own `IMarkdownReportRenderer`/`IJsonReportRenderer`
instead of referencing this package — the core engine and the Agent never assume this project is the
reporting source, they only depend on the shared contracts in `ValidationEngine.Models`.

## License

MIT — see [LICENSE](https://github.com/TroyCrowe-TCCoder/ValidationEngine/blob/main/LICENSE).
