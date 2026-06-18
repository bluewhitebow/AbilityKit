param(
    [switch]$NoBuild,
    [string]$Configuration = 'Debug',
    [int]$TcpPort = 41001
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptDir '..\..\..')
$project = Join-Path $repoRoot 'Server\Orleans\src\AbilityKit.Orleans.ShooterSmoke\AbilityKit.Orleans.ShooterSmoke.csproj'

if (-not (Test-Path $project)) {
    throw "Shooter smoke project was not found: $project"
}

$commonArgs = @(
    '-p:UseSharedCompilation=false',
    '-p:nodeReuse=false'
)

if (-not $NoBuild) {
    Write-Host "Building Shooter smoke project..."
    dotnet build $project -c $Configuration @commonArgs
}

Write-Host "Running Shooter TCP Gateway smoke on 127.0.0.1:$TcpPort..."
$runArgs = @(
    'run',
    '--project', $project,
    '-c', $Configuration
)

if ($NoBuild) {
    $runArgs += '--no-build'
}

$runArgs += $commonArgs
$runArgs += @('--', '--tcp-port', $TcpPort)
& dotnet @runArgs
