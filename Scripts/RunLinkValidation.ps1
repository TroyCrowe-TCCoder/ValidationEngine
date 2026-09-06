param(
	[Parameter(Mandatory = $false)]
	[string]$RepositoryRoot,

	[Parameter(Mandatory = $false)]
	[switch]$FullAudit,

	[Parameter(Mandatory = $false)]
	[string]$BaseBranch,

	[Parameter(Mandatory = $false)]
	[string]$App,

	[Parameter(Mandatory = $false)]
	[string]$OutputPath,

	[Parameter(Mandatory = $false)]
	[int]$Concurrency,

	[Parameter(Mandatory = $false)]
	[int]$TimeoutSeconds
)

# Thin wrapper — ValidationEngine
#
# Invokes the compiled ValidationEngine.Link console app (ValidationEngine.Link/ValidationEngine.Link.csproj)
# against the target repository. This script contains no validation logic of its own; it only
# ensures the published binary exists and is not older than its source before running it.
#
# Replaces the previous markdown-link-check (npx)-based RunMarkdownLinkCheck.ps1: that approach
# spawned a fresh Node/npx process per Markdown file, which dominated total validation run time
# on repositories with many documents. ValidationEngine.Link performs the equivalent internal
# (relative path/anchor) and external (HTTP HEAD/GET) link checks natively in-process, in
# parallel, with no per-file process-spawn cost.
#
# Extend ValidationEngine.Link/*.cs instead of this script — all governed repositories share the
# same compiled link engine.

$ErrorActionPreference = 'Stop'

if (-not $RepositoryRoot)
{
	$RepositoryRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
}

$engineRoot = Join-Path $PSScriptRoot '..' 'ValidationEngine.Link'
$projectFile = Join-Path $engineRoot 'ValidationEngine.Link.csproj'
$publishedExePath = Join-Path $engineRoot 'bin' 'Release' 'net10.0' 'ValidationEngine.Link.exe'

$sourceFiles = Get-ChildItem -Path $engineRoot -Recurse -File -Include *.cs, *.csproj
$newestSourceWriteTime = ($sourceFiles | Measure-Object -Property LastWriteTimeUtc -Maximum).Maximum

$needsBuild = -not (Test-Path $publishedExePath)
if (-not $needsBuild)
{
	$publishedExeWriteTime = (Get-Item $publishedExePath).LastWriteTimeUtc
	$needsBuild = $newestSourceWriteTime -gt $publishedExeWriteTime
}

if ($needsBuild)
{
	Write-Host 'ValidationEngine.Link binary is missing or stale. Publishing...'
	dotnet publish $projectFile -c Release -o (Join-Path $engineRoot 'bin' 'Release' 'net10.0')
	if ($LASTEXITCODE -ne 0)
	{
		Write-Error 'Failed to publish ValidationEngine.Link.'
		exit 1
	}
}

$engineArguments = @('-RepositoryRoot', $RepositoryRoot)
if ($FullAudit)
{
	$engineArguments += '--full-audit'
}
if ($BaseBranch)
{
	$engineArguments += @('--base-branch', $BaseBranch)
}
if ($App)
{
	$engineArguments += @('--app', $App)
}
if ($OutputPath)
{
	$engineArguments += @('--output', $OutputPath)
}
if ($Concurrency -gt 0)
{
	$engineArguments += @('--concurrency', $Concurrency)
}
if ($TimeoutSeconds -gt 0)
{
	$engineArguments += @('--timeout', $TimeoutSeconds)
}

& $publishedExePath @engineArguments
exit $LASTEXITCODE
