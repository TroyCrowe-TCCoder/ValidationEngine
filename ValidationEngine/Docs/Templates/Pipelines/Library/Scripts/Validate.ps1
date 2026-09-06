param(
	[switch]$Pack,
	[switch]$SkipClean
)

$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')

# Remove TestResults folders produced by previous test runs unless the caller opts out.
# These accumulate a new GUID-named subfolder on every run and are not source-controlled.
if (-not $SkipClean)
{
	$testResultDirs = Get-ChildItem -Path $root -Recurse -Directory -Filter 'TestResults' |
		Where-Object { $_.FullName -notlike '*\bin\*' -and $_.FullName -notlike '*\obj\*' }

	foreach ($dir in $testResultDirs)
	{
		Write-Host "Cleaning $($dir.FullName)..."
		Remove-Item -Path $dir.FullName -Recurse -Force
	}
}
$solution = Get-ChildItem -Path $root -Filter *.sln | Select-Object -First 1
$project = Get-ChildItem -Path $root -Filter *.csproj | Select-Object -First 1

if (-not $solution -and -not $project)
{
	Write-Error 'No solution file or project file found in repository root.'
	exit 1
}

$buildTarget = if ($solution) { $solution.FullName } else { $project.FullName }
$buildTargetName = if ($solution) { $solution.Name } else { $project.Name }

Write-Host "Restoring $buildTargetName..."
dotnet restore $buildTarget
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Building $buildTargetName..."
dotnet build $buildTarget --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$testProjects = Get-ChildItem -Path $root -Recurse -File -Include *.Tests.csproj,*Test*.csproj |
	Where-Object { $_.FullName -notlike '*\bin\*' -and $_.FullName -notlike '*\obj\*' }

if ($testProjects)
{
	Write-Host "Testing $buildTargetName..."
	dotnet test $solution.FullName --configuration Release --no-build --verbosity normal
	if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
else
{
	Write-Warning 'No test projects were found. Skipping dotnet test for this repository.'
}

if ($Pack)
{
	$artifacts = Join-Path $root 'artifacts'
	New-Item -ItemType Directory -Path $artifacts -Force | Out-Null
	Write-Host "Packing solution outputs to $artifacts..."
	dotnet pack $buildTarget --configuration Release --no-build --output $artifacts
	if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Write-Host 'Class library validation completed successfully.'
