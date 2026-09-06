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

| Project | Description |
|---|---|
| `ValidationEngine` | Core deterministic mechanical rule engine and orchestration. |
| `ValidationEngine.Agent` | AI-assisted evaluation for manual-only / judgment-based rules. |
| `ValidationEngine.Link` | Markdown link validation (internal + external links). |
| `ValidationEngine.Analyzers` | Roslyn analyzer package family (compile-time enforcement), organized by domain folders/namespaces. |

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

This repository does not itself contain the standards corpus. It expects a sibling clone of
the `GlobalStandards` repository, referenced via relative path:

```
../GlobalStandards/Docs/Standards
```

Clone both repositories side by side:

```
repos/
  GlobalStandards/
  ValidationEngine/
```

Standards rules remain the authoritative "fuel source" — human-readable and agent-readable
markdown files that can be added to, edited, or removed independently of this tooling.

## License

MIT — see [LICENSE](LICENSE). Contributions are welcome; all changes are gated on maintainer
approval.
