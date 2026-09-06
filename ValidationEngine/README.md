# ValidationEngine

Core deterministic mechanical rule engine and orchestrator for the
[ValidationEngine](https://github.com/TroyCrowe-TCCoder/ValidationEngine) standards validation
suite, distributed as the `validation-engine` .NET global tool.

## What it does

`validation-engine` validates a target repository against a standards corpus (see
[Standards Source](https://github.com/TroyCrowe-TCCoder/ValidationEngine#standards-source)):

- Runs deterministic, mechanical rule checks against the repository.
- Orchestrates optional sibling tools — `validation-engine-agent` for AI-assisted manual-only
  rule evaluation and `validation-engine-link` for markdown link validation — when they are
  installed and applicable to the current run.
- Renders results as Markdown and JSON via `ValidationEngine.Reporting`, writes them to
  `Working/ValidationDiscrepancies.md` / `.json`, and retains a pruned run history under
  `Working/ValidationHistory/`.
- Exits with a status code reflecting the outcome: `0` clean, `1` violations or manual-review
  items present, `2` engine errors.

## Installing

```bash
dotnet tool install --global ValidationEngine
```

## Usage

Run from the root of the repository you want to validate:

```bash
validation-engine
```

| Argument | Description |
|---|---|
| `-RepositoryRoot <path>` | Repository to validate. Defaults to the current directory. |
| `-GlobalStandardsRoot <path>` | Standards corpus location override; falls back to `GLOBALSTANDARDS_ROOT` env var, then `"standardsPath"` in `validationengine.config.json`, then `Docs/Standards` under the repository root. |
| `-Mode <Manual\|Pr\|...>` | Run mode; controls which rule set(s) execute. Defaults to `Manual`. |
| `-TargetBranch <branch>` | Base branch for PR-mode diff scoping. |

## How it fits together

`ValidationEngine` depends on `ValidationEngine.Models` (shared contracts) and
`ValidationEngine.Reporting` (default renderers, used only because this project's `Program.cs` is
a composition root — any other consumer is free to supply its own renderer implementations).
`ValidationEngine.Agent` and `ValidationEngine.Link` are launched as external sibling processes,
not compiled dependencies, so each can be installed, updated, or omitted independently.

## License

MIT — see [LICENSE](https://github.com/TroyCrowe-TCCoder/ValidationEngine/blob/main/LICENSE).
