# Onboarding a Repository to ValidationEngine

This is the step-by-step process for setting up a **new consumer repository** to use
ValidationEngine. It is written for someone who has just discovered/downloaded the tool and
needs to know exactly what to install and configure — no prior context assumed.

If you are looking for product architecture, run-mode arguments, or AI-provider configuration
reference, see [README.md](README.md) instead. This document is the onboarding *sequence*; the
README is the reference manual.

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) matching or exceeding `net10.0` installed.
- Git installed and the target repository already cloned locally.
- No clone of this (`ValidationEngine`) repository is required. ValidationEngine is consumed
  entirely as published .NET global tools — you never build it from source to use it.

## Step 1 — Install the Core Tool

```bash
dotnet tool install --global ValidationEngine
```

Confirm it resolved onto your `PATH`:

```bash
validation-engine -RepositoryRoot . -Mode Manual
```

If this runs (even if it reports violations), the core tool is installed correctly. If
`validation-engine` is not recognized, restart your shell so the global tools path is picked up.

## Step 2 — Install Optional Sibling Tools (as needed)

These are independent add-ons. Only install what your repository needs; `validation-engine`
detects and launches whichever of these are present on `PATH` automatically — no configuration
required to "turn them on."

```bash
dotnet tool install --global ValidationEngine.Agent   # AI-assisted manual-only rule review
dotnet tool install --global ValidationEngine.Link    # Markdown internal/external link validation
```

If you skip one, `validation-engine` simply runs without it — the run does not fail. Manual-only
rules go unevaluated (reported as `AGT-001`) if the Agent isn't installed or isn't configured;
link findings are simply absent if Link isn't installed.

## Step 3 — Vendor a Standards Corpus

By default `validation-engine` looks for the standards corpus under `Docs/Standards` relative to
the repository being validated. Populate that folder with your standards markdown files (or the
files vendored from your organization's canonical standards source).

If your standards live somewhere else, override the location using one of, in priority order:

1. `-GlobalStandardsRoot <path>` command-line argument.
2. `GLOBALSTANDARDS_ROOT` environment variable.
3. `"standardsPath"` in `validationengine.config.json` at the repository root.

No sibling repository checkout is required for any of these options.

## Step 4 — (Optional) Configure AI-Assisted Manual Review

Only needed if you installed `ValidationEngine.Agent` in Step 2 and want manual-only rules
evaluated automatically rather than reported as unresolved.

Add `appsettings.json` at the repository root — see
[Configuring AI-Assisted Review](README.md#configuring-ai-assisted-review) in the README for the
full schema. If you installed `ValidationEngine.Agent` but do not want AI-assisted review yet,
ship the file with an empty `Providers` array to explicitly opt out (an entirely missing file is
reported as an engine error, not a silent skip).

## Step 5 — Wire Up Local Enforcement (Pre-Commit Hook + Manual Script)

A repository onboarding to ValidationEngine should expose exactly **two** entry points:

1. A pre-commit hook that blocks a commit on validation failure.
2. A manual script a developer can run on demand.

Both are thin wrappers around the installed tool — no validation logic belongs in either file.

**`.githooks/pre-commit`** (POSIX shell, checked into source control):

```sh
#!/bin/sh
repo_root="$(git rev-parse --show-toplevel)"
echo "Running pre-commit validation (validation-engine)..."
validation-engine -RepositoryRoot "$repo_root" -Mode System
status=$?
if [ $status -ne 0 ]; then
  echo ""
  echo "Pre-commit validation FAILED. Commit blocked."
  echo "Fix the discrepancies listed above, then try committing again."
  exit 1
fi
echo "Pre-commit validation passed."
exit 0
```

**`Validate.ps1`** (repository root, for on-demand manual runs):

```powershell
$repoRoot = git rev-parse --show-toplevel
Write-Host "Running validation (validation-engine)..."
validation-engine -RepositoryRoot $repoRoot -Mode Manual
exit $LASTEXITCODE
```

A hook checked into `.githooks/` is not active until a contributor points Git at it. Provide a
one-time setup script (e.g. `SetupHooks.ps1`) that a contributor runs once after cloning:

```powershell
$repoRoot = git rev-parse --show-toplevel
Push-Location $repoRoot
try {
	git config core.hooksPath .githooks
	git update-index --chmod=+x -- .githooks/pre-commit 2>$null | Out-Null
	Write-Host "Git hooks enabled. core.hooksPath set to .githooks in $repoRoot" -ForegroundColor Green
}
finally {
	Pop-Location
}
```

Do not add checks for a sibling `GlobalStandards` (or any other) repository clone in this script
— none is required by the installed-tool model.

## Step 6 — Wire Up CI/PR Gating (Pipeline)

Do not check out this (`ValidationEngine`) repository as a pipeline resource. The pipeline only
needs to install the published tool(s) and invoke `validation-engine` as a step, exactly like a
developer's local hook does.

### Azure DevOps (YAML pipelines)

Minimum steps for a stage/job that gates a PR:

```yaml
steps:
- checkout: self
  fetchDepth: 0   # required so ValidationEngine can diff against the target branch

- task: PowerShell@2
  displayName: Install ValidationEngine global tool
  inputs:
	pwsh: true
	targetType: inline
	script: |
	  dotnet tool install --global ValidationEngine
	  # Install sibling tools too if this repository uses them:
	  # dotnet tool install --global ValidationEngine.Agent
	  # dotnet tool install --global ValidationEngine.Link

- task: PowerShell@2
  name: RunValidation
  displayName: Run full standards validation
  inputs:
	pwsh: true
	targetType: inline
	script: |
	  $repoRoot = '$(Build.SourcesDirectory)'
	  validation-engine -RepositoryRoot $repoRoot -Mode System -TargetBranch origin/dev
	  Write-Host "##vso[task.setvariable variable=ValidationExitCode;isOutput=true]$LASTEXITCODE"

- task: PowerShell@2
  displayName: Enforce validation result
  inputs:
	pwsh: true
	targetType: inline
	script: |
	  $code = '$(RunValidation.ValidationExitCode)'
	  if ($code -ne '0') {
		Write-Error 'Standards validation failed. See RunValidation step log for blocking violations.'
		exit 1
	  }
	  Write-Host 'Standards validation passed (blocking checks).'
```

Notes:

- `fetchDepth: 0` is required — `-Mode System -TargetBranch origin/dev` diffs against the target
  branch, and a shallow checkout will not have that history available.
- The exit code is captured into an output variable in a separate step (rather than gating
  directly on the run step's own result) so a later step can still post PR comments or upload
  artifacts even when validation fails — see the reusable template at
  `GlobalStandards/Docs/Templates/Pipelines/WebApiWebApp/Shared/StandardsValidation.yml` for a
  complete example that also posts manual-review warnings as a PR comment.
- Reuse that template directly (as a pipeline stage template reference) rather than
  hand-rolling the steps above per repository, if your organization already maintains it.

### GitHub Actions

```yaml
name: Standards Validation

on:
  pull_request:
	branches: [dev]

jobs:
  validate:
	runs-on: windows-latest
	steps:
	  - name: Checkout
		uses: actions/checkout@v4
		with:
		  fetch-depth: 0

	  - name: Setup .NET
		uses: actions/setup-dotnet@v4
		with:
		  dotnet-version: "10.0.x"

	  - name: Install ValidationEngine
		run: dotnet tool install --global ValidationEngine

	  - name: Run standards validation
		run: |
		  validation-engine -RepositoryRoot "${{ github.workspace }}" -Mode System -TargetBranch origin/${{ github.base_ref }}
```

A non-zero exit code from `validation-engine` fails the step (and therefore the job) automatically
— no separate enforcement step is required in GitHub Actions, since `run:` steps already fail the
job on a non-zero exit code.

### Handling Findings in CI

`validation-engine` writes `Working/ValidationDiscrepancies.md` / `.json` whenever there are
violations, manual-review items, or engine errors. In CI you typically want to:

- Publish `Working/ValidationDiscrepancies.md` as a build artifact so a failed run's findings are
  reviewable without re-running locally.
- Optionally parse `Working/ValidationDiscrepancies.json` (or the `## Manual Review Required`
  section of the `.md` file, as the Azure DevOps template above does) to post PR comments for
  items that pass the mechanical gate but still need human judgment.

### Exit Codes

| Code | Meaning | CI behavior |
|---|---|---|
| `0` | Clean — no violations, manual-review items, or engine errors. | Gate passes. |
| `1` | Violations and/or manual-review items present. | Gate should fail the build/PR check. |
| `2` | Engine error (e.g. missing standards corpus, sibling tool crashed). | Gate should fail the build/PR check; investigate the engine error, not the target repository's code. |

## Step 7 — Verify Readiness

Before considering onboarding complete, confirm:

- [ ] `validation-engine -RepositoryRoot . -Mode Manual` runs successfully from the repository root.
- [ ] The pre-commit hook is active (`git config core.hooksPath` reports `.githooks`) and blocks a
	  commit when a known violation is introduced.
- [ ] `Validate.ps1` (or equivalent) runs the same tool on demand.
- [ ] The CI pipeline stage installs and runs `validation-engine` without checking out this
	  repository.
- [ ] The CI checkout step fetches full history (`fetchDepth: 0` / `fetch-depth: 0`) so
	  target-branch diffing works.
- [ ] If `ValidationEngine.Agent` is installed, `appsettings.json` exists at the repository root
	  (even if every provider is inactive).

## Troubleshooting

| Symptom | Likely Cause |
|---|---|
| `validation-engine` not found after install | Shell needs to be restarted to pick up the global tools `PATH` entry. |
| Manual-only rules reported as `AGT-001` | `ValidationEngine.Agent` is not installed, or is installed but has no active provider configured. |
| No link findings even though a link is broken | `ValidationEngine.Link` is not installed. |
| Link validation reports issues in an unrelated sibling repository | You are running a stale/legacy script rather than the installed `validation-engine` tool — remove any leftover wrapper scripts and call `validation-engine` directly. |
| Engine error about missing `appsettings.json` | `ValidationEngine.Agent` is installed, but no `appsettings.json` exists at the target repository root. Add one (see Step 4). |
| CI run reports exit code `2` with a target-branch/diff error | Checkout step used a shallow clone. Set `fetchDepth: 0` (Azure DevOps) or `fetch-depth: 0` (GitHub Actions). |
| CI gate doesn't block a PR despite exit code `1`/`2` | The pipeline isn't checking `$LASTEXITCODE` (or the shell step's own exit) — see Step 6's "Enforce validation result" step for Azure DevOps; GitHub Actions `run:` steps fail automatically on non-zero exit. |
