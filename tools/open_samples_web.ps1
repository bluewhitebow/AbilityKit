param(
    [string]$OutputDirectory = '',
    [ValidateSet('instant', 'simulated', 'realtime')]
    [string]$Mode = 'instant',
    [switch]$NoBuild,
    [switch]$NoOpen
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$project = Join-Path $repoRoot 'src\AbilityKit.Samples\AbilityKit.Samples.csproj'

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot 'sample-web'
}

$arguments = @('run')
if ($NoBuild.IsPresent) {
    $arguments += '--no-build'
}

$arguments += @(
    '--project',
    $project,
    '--',
    '--web',
    $OutputDirectory,
    '--mode',
    $Mode
)

Write-Host '=== AbilityKit Samples Web Export ===' -ForegroundColor Cyan
Write-Host ("Project: {0}" -f $project)
Write-Host ("Output:  {0}" -f $OutputDirectory)
Write-Host ("Mode:    {0}" -f $Mode)

& dotnet @arguments
$exitCode = $LASTEXITCODE
if ($exitCode -ne 0) {
    throw "Sample web export failed with exit code $exitCode"
}

$indexPath = Join-Path $OutputDirectory 'index.html'
if (-not (Test-Path $indexPath)) {
    throw "Sample web export did not create index.html: $indexPath"
}

$resolvedIndexPath = (Resolve-Path $indexPath).Path
Write-Host ("Generated: {0}" -f $resolvedIndexPath) -ForegroundColor Green

if (-not $NoOpen.IsPresent) {
    Write-Host 'Opening in the default browser...'
    Start-Process -FilePath $resolvedIndexPath
}

