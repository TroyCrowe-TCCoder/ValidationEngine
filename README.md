# ValidationEngine

Automated repository standards validation suite: deterministic mechanical rule checks,
AI-assisted manual-only rule evaluation, markdown link validation, and Roslyn compile-time
analyzers, all driven by a shared, editable standards corpus.

## Purpose

ValidationEngine enforces the rules defined in a companion standards repository (see
[Standards Source](#standards-source) below) across a codebase: at compile time via analyzers,
at validation time via deterministic and AI-assisted rule engines, and for documentation via
link validation.

## Projects

| Project | Package type | Description |
|---|---|---|
| `ValidationEngine` | .NET tool (`validation-engine`) | Core deterministic mechanical rule engine and orchestration; the entry point for a validation run. |
| `ValidationEngine.Agent` | .NET tool (`validation-engine-agent`) | AI-assisted evaluation for manual-only / judgment-based rules. Optional add-on invoked by the core engine. |
| `ValidationEngine.Link` | .NET tool (`validation-engine-link`) | Markdown link validation (internal + external links). Can run standalone or as an add-on. |
| `ValidationEngine.Models` | NuGet library | Shared domain/reporting contracts (`ValidationReport`, `RuleFinding`, etc.) with no dependency on any other project. |
| `ValidationEngine.Reporting` | NuGet library | Default Markdown/JSON report renderers; a plug-in, not a required dependency. |
| `ValidationEngine.Analyzers` | NuGet analyzer package | Roslyn analyzer package family (compile-time enforcement), organized by domain folders/namespaces. |

## Configuring AI-Assisted Review

`ValidationEngine.Agent` evaluates manual-only / judgment-based rules using an AI provider. It
requires an `appsettings.json` file at the root of the repository being validated (the file is
required — the AI provider configuration inside it is not; a repository can ship one with every
provider inactive to explicitly opt out).

Add an `appsettings.json` at the target repository's root with a `"ValidationAgent"` section:

```json
{
  "ValidationAgent": {
    "Providers": [
      {
        "Name": "AzureOpenAI-Validation",
        "Type": "AzureOpenAI",
        "Purpose": "Validation",
        "IsActive": true,
        "Endpoint": "https://<your-resource>.openai.azure.com/",
        "DeploymentName": "<your-deployment-name>",
        "ApiVersion": "2024-10-21"
      },
      {
        "Name": "AzureOpenAI-Explain",
        "Type": "AzureOpenAI",
        "Purpose": "Explain",
        "IsActive": true,
        "Endpoint": "https://<your-resource>.openai.azure.com/",
        "DeploymentName": "<your-deployment-name>",
        "ApiVersion": "2024-10-21"
      }
    ]
  }
}
```

Notes:

- `Providers` is an array — zero, one, or many entries can be active at once (`IsActive: true`),
  even across multiple purposes. There is no single "ActiveProvider" selector.
- `Purpose` must be `Validation` or `Explain` (see `ValidationEngine.Agent.AgentPurpose`).
  `Validation` providers evaluate manual-only rules; `Explain` providers optionally attach
  rationale to findings already produced by a `Validation` provider.
- `Type` must currently be `AzureOpenAI` (see `ValidationEngine.Agent.Constants.ProviderTypes`).
- Secrets (API keys) are never stored in `appsettings.json`. Each active provider resolves its
  key from an environment variable named after the provider's `Name` (e.g. `AzureOpenAI-Validation`).
- If the file is missing entirely, the Agent reports an engine error. If the file exists but no
  provider resolves to active, manual-only rules are reported as `AGT-001` ("not evaluated")
  instead of being silently skipped.

## Standards Source

The standards corpus is vendored directly into this repository under `Docs/Standards`. Those
markdown files are the authoritative "fuel source" — human-readable and agent-readable rule
definitions that drive both the deterministic and AI-assisted engines. They can be added to,
edited, or removed independently of the engine code itself.

By default, the engine resolves the standards root from `Docs/Standards` relative to the
repository being validated. If you are validating a different repository, or you keep the
standards corpus elsewhere, point the engine at it explicitly using one of, in priority order:

1. The `-GlobalStandardsRoot <path>` command-line argument.
2. The `GLOBALSTANDARDS_ROOT` environment variable.
3. `"standardsPath"` in `validationengine.config.json` at the target repository's root.

## Installing the Tools

Each executable component is published as an independent
[.NET tool](https://learn.microsoft.com/dotnet/core/tools/global-tools) so consumers only install
what they need:

```bash
dotnet tool install --global ValidationEngine          # validation-engine (core orchestrator)
dotnet tool install --global ValidationEngine.Agent     # validation-engine-agent (AI-assisted manual review)
dotnet tool install --global ValidationEngine.Link      # validation-engine-link (markdown link validation)
```

`ValidationEngine.Models`, `ValidationEngine.Reporting`, and `ValidationEngine.Analyzers` are
regular NuGet libraries/analyzers referenced from a project rather than installed as tools — see
their own package READMEs for details.

## Running the Core Engine

From the root of the repository you want to validate:

```bash
validation-engine
```

Common arguments:

| Argument | Description |
|---|---|
| `-RepositoryRoot <path>` | Repository to validate. Defaults to the current directory. |
| `-GlobalStandardsRoot <path>` | Standards corpus location override (see above). |
| `-Mode <Manual\|Pr\|...>` | Run mode; controls which rule set(s) execute. Defaults to `Manual`. |
| `-TargetBranch <branch>` | Base branch for PR-mode diff scoping. |

The engine writes `Working/ValidationDiscrepancies.md` and `.json` (plus a pruned history under
`Working/ValidationHistory/`) whenever violations, manual-review items, or engine errors are
found, and prints the Markdown report to the console. The process exit code is `0` (clean),
`1` (violations/manual-review items present), or `2` (engine errors).

`ValidationEngine` invokes `ValidationEngine.Agent` and `ValidationEngine.Link` as external
sibling tool processes when they are installed and applicable to the current run — none of the
three is a hard compile-time dependency of another beyond `ValidationEngine.Models`.

## Running Link Validation Standalone

`validation-engine-link` can also be run directly, independent of the core engine:

```bash
validation-engine-link --full-audit
```

| Argument | Description |
|---|---|
| `-RepositoryRoot <path>` | Repository to scan. Defaults to the current directory. |
| `--full-audit` | Scan every markdown file instead of only files changed vs. a base branch. |
| `--base-branch <branch>` | Base branch to diff against when not doing a full audit. |
| `--app <name>` | Application name attached to reported issues. |
| `--output <path>` | Write issues as JSON to the given path. |
| `--concurrency <n>` | Max concurrent link checks. Defaults to processor count. |
| `--timeout <seconds>` | Per-external-link HTTP timeout. Defaults to 10. |

## Running AI-Assisted Manual Review

`validation-engine-agent` evaluates manual-only / judgment-based rules (see
[Configuring AI-Assisted Review](#configuring-ai-assisted-review) above) and is normally invoked
automatically by `validation-engine`. It can also be run standalone against a repository that has
an `appsettings.json` configured with at least one active provider.

## License

MIT — see [LICENSE](LICENSE). Contributions are welcome; all changes are gated on maintainer
approval.
