param(
	[Parameter(Mandatory = $false)]
	[string]$RepositoryRoot
)

# Thin wrapper — ValidationEngine
#
# Runs markdownlint-cli2 (pinned via npx) against the target repository's Markdown content using
# its own .markdownlint-cli2.jsonc config, if present. When $RepositoryRoot is omitted, defaults
# to this repository's root (ValidationEngine's own Docs/Standards).
#
# Called from Scripts/RunValidationEngine.ps1 (pre-commit/CI), so all governed repositories share
# identical Markdown/prose enforcement behavior.
#
# Deliberately NOT implemented in ValidationEngine/*.cs: markdownlint is a purpose-built,
# well-maintained tool for this class of check.

$ErrorActionPreference = 'Stop'

if (-not $RepositoryRoot)
{
	$RepositoryRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
}

$configPath = Join-Path $RepositoryRoot '.markdownlint-cli2.jsonc'
if (-not (Test-Path $configPath))
{
	Write-Host 'No .markdownlint-cli2.jsonc found at repository root. Skipping markdownlint.'
	exit 0
}

Write-Host 'Running markdownlint-cli2 against repository Markdown content (see .markdownlint-cli2.jsonc)...'

Push-Location $RepositoryRoot
try
{
	npx --yes markdownlint-cli2@0.13.0
	exit $LASTEXITCODE
}
finally
{
	Pop-Location
}
