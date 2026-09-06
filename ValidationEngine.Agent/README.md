# ValidationEngine.Agent

AI-assisted manual review add-on for the
[ValidationEngine](https://github.com/TroyCrowe-TCCoder/ValidationEngine) standards validation
suite, distributed as the `validation-engine-agent` .NET global tool.

## What it does

Deterministic rule checks can't evaluate judgment-based standards (readability, intent, "does
this comment actually explain why"). `validation-engine-agent` closes that gap by sending
manual-only rules from the standards corpus to a configured AI provider for evaluation, and
reporting findings back through the same `ValidationReport` contract the core engine uses.

It is normally invoked automatically by `validation-engine` as a sibling process, but can also be
run standalone against any repository with a valid `appsettings.json`.

## Installing

```bash
dotnet tool install --global ValidationEngine.Agent
```

## Configuration (required)

The Agent requires an `appsettings.json` file at the root of the repository being validated. The
file itself is required — having an active provider inside it is not; a repository can ship one
with every provider inactive to explicitly opt out.

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

## How it fits together

`ValidationEngine.Agent` references `ValidationEngine` and `ValidationEngine.Models`, and uses
`ValidationEngine.Reporting`'s default renderers as a composition-root convenience (any consumer
is free to supply its own). It is launched by the core engine as an external process, not a
compile-time dependency in the other direction, so it can be installed, updated, or omitted
independently.

## License

MIT — see [LICENSE](https://github.com/TroyCrowe-TCCoder/ValidationEngine/blob/main/LICENSE).
