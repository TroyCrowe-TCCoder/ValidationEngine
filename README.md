# ValidationEngine

Automated repository standards validation suite: deterministic mechanical rule checks,
AI-assisted manual-only rule evaluation, markdown link validation, and Roslyn compile-time
analyzers, all driven by a shared, editable standards corpus.

## What It Is

ValidationEngine is a suite of independently distributable .NET tools and libraries that enforce
the rules defined in a shared, editable standards corpus (see
[Standards Source](#standards-source) below) against a codebase. Rather than a single monolithic
application, it is a small family of composable components — a core orchestrator, optional
add-on tools, shared contracts, and compile-time analyzers — that can be installed and versioned
independently while still working together as a cohesive whole.

## What It Does

- Runs deterministic, mechanical rule checks against a target repository (coding, security,
  database, and similar standards that can be verified programmatically).
- Evaluates manual-only / judgment-based rules using a configured AI provider, for standards
  that require human-like judgment (e.g. "does this explain *why*, not just *what*").
- Validates internal and external links across markdown documentation.
- Enforces a subset of the same standards at compile time, directly in the IDE and on build,
  via Roslyn analyzers — catching violations before a validation run ever happens.
- Renders results as Markdown and JSON reports, retains a pruned run history, and produces a
  process exit code suitable for CI gating.

## Its Modularity

| Project | Package type | Description |
|---|---|---|
| `ValidationEngine` | .NET tool (`validation-engine`) | Core deterministic mechanical rule engine and orchestration; the entry point for a validation run. |
| `ValidationEngine.Agent` | .NET tool (`validation-engine-agent`) | AI-assisted evaluation for manual-only / judgment-based rules. Optional add-on invoked by the core engine. |
| `ValidationEngine.Link` | .NET tool (`validation-engine-link`) | Markdown link validation (internal + external links). Can run standalone or as an add-on. |
| `ValidationEngine.Models` | NuGet library | Shared domain/reporting contracts (`ValidationReport`, `RuleFinding`, etc.) with no dependency on any other project. |
| `ValidationEngine.Reporting` | NuGet library | Default Markdown/JSON report renderers; a plug-in, not a required dependency. |
| `ValidationEngine.Analyzers` | NuGet analyzer package | Roslyn analyzer package family (compile-time enforcement), organized by domain folders/namespaces. |

Each component is installed and versioned independently — a consumer only takes what it needs.
`ValidationEngine.Agent` and `ValidationEngine.Link` are launched by the core engine as external
sibling processes, not compile-time dependencies, so either can be added, updated, or omitted
without touching the others. `ValidationEngine.Models` and `ValidationEngine.Reporting` are the
only shared compiled dependencies, and `ValidationEngine.Reporting` is itself a swappable
plug-in — any consumer can supply its own renderer instead. `ValidationEngine.Analyzers` is
fully independent of the rest of the suite; it enforces the same standards corpus at compile
time only.

## How It Works

1. `validation-engine` resolves the standards corpus (see [Standards Source](#standards-source))
   and the repository to validate, then runs its deterministic rule set against it.
2. If `ValidationEngine.Agent` is installed and applicable rules require it, the core engine
   launches `validation-engine-agent` as a sibling process to evaluate manual-only rules via a
   configured AI provider (see [Configuring AI-Assisted Review](#configuring-ai-assisted-review)).
3. If `ValidationEngine.Link` is installed and applicable, the core engine launches
   `validation-engine-link` as a sibling process to validate markdown links; it can also be run
   entirely standalone.
4. All findings are merged into a single `ValidationReport` (from `ValidationEngine.Models`) and
   rendered via `ValidationEngine.Reporting`'s Markdown/JSON renderers (or a custom
   implementation) into `Working/ValidationDiscrepancies.md` / `.json`, with a pruned run history
   retained under `Working/ValidationHistory/`.
5. Independently of any validation run, `ValidationEngine.Analyzers` — if referenced by a
   project — flags the same class of standards violations live in the IDE and on every build.
6. The process exits `0` (clean), `1` (violations or manual-review items present), or `2`
   (engine errors), making the core tool suitable for CI/PR gating.

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
