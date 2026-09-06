param(
	[Parameter(Mandatory = $true)]
	[string]$RepositoryRoot,

	[Parameter(Mandatory = $false)]
	[string]$GlobalStandardsRoot,

	[Parameter(Mandatory = $false)]
	[ValidateSet('Manual', 'System')]
	[string]$Mode = 'System',

	[Parameter(Mandatory = $false)]
	[string]$TargetBranch
)

# Thin wrapper — ValidationEngine
#
# Invokes the compiled ValidationEngine console app (ValidationEngine/ValidationEngine.csproj)
# against the target repository. This script contains no validation logic of its own; it only
# ensures the published binary exists and is not older than its source before running it.
#
# -GlobalStandardsRoot is optional: when omitted, the engine resolves its standards source via
# GLOBALSTANDARDS_ROOT, validationengine.config.json in the target repository, or its own
# bundled Docs/Standards (see ValidationEngine/Program.cs). No repository name is hardcoded here.
#
# Extend ValidationEngine/*.cs instead of this script — all governed repositories share the
# same compiled engine.

$ErrorActionPreference = 'Stop'

$engineRoot = Join-Path $PSScriptRoot '..' 'ValidationEngine'
$projectFile = Join-Path $engineRoot 'ValidationEngine.csproj'
$publishedExePath = Join-Path $engineRoot 'bin' 'Release' 'net10.0' 'ValidationEngine.exe'

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
	Write-Host 'ValidationEngine binary is missing or stale. Publishing...'
	dotnet publish $projectFile -c Release -o (Join-Path $engineRoot 'bin' 'Release' 'net10.0')
	if ($LASTEXITCODE -ne 0)
	{
		Write-Error 'Failed to publish ValidationEngine.'
		exit 1
	}
}

$engineArguments = @('-RepositoryRoot', $RepositoryRoot, '-Mode', $Mode)
if ($GlobalStandardsRoot)
{
	$engineArguments += @('-GlobalStandardsRoot', $GlobalStandardsRoot)
}
if ($TargetBranch)
{
	$engineArguments += @('-TargetBranch', $TargetBranch)
}

& $publishedExePath @engineArguments
$engineExitCode = $LASTEXITCODE
if ($engineExitCode -ne 0)
{
	exit $engineExitCode
}

& (Join-Path $PSScriptRoot 'RunMarkdownLint.ps1') -RepositoryRoot $RepositoryRoot
if ($LASTEXITCODE -ne 0)
{
	exit $LASTEXITCODE
}

& (Join-Path $PSScriptRoot 'RunLinkValidation.ps1') -RepositoryRoot $RepositoryRoot
exit $LASTEXITCODE
