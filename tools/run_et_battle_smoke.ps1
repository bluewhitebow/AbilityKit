param(
    [string]$ConfigPath = 'tools\et-battle-smoke.config.json',
    [int]$SmokeFrames,
    [string]$SmokeCasePath,
    [int]$MinBattleFrames,
    [int]$TimeoutMilliseconds,
    [int]$SleepMilliseconds,
    [int]$DrainFrames,
    [int]$ConsistencyRuns,
    [switch]$NoBuild,
    [switch]$SkipConfigValidation,
    [switch]$KeepOutput
)

$ErrorActionPreference = 'Stop'
$scriptBoundParameters = @{} + $PSBoundParameters

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
if ([System.IO.Path]::IsPathRooted($ConfigPath)) {
    $resolvedConfigPath = $ConfigPath
}
else {
    $resolvedConfigPath = Join-Path $repoRoot $ConfigPath
}

if (-not (Test-Path $resolvedConfigPath)) {
    throw "Smoke config file not found: $resolvedConfigPath"
}

$smokeConfig = Get-Content -Path $resolvedConfigPath -Raw | ConvertFrom-Json

function Test-ConfigProperty {
    param(
        [object]$Config,
        [string]$Name
    )

    return $null -ne $Config.PSObject.Properties[$Name]
}

function Resolve-IntSetting {
    param(
        [string]$Name,
        [int]$CommandValue,
        [int]$Fallback
    )

    if ($scriptBoundParameters.ContainsKey($Name)) {
        return [int]$CommandValue
    }

    if (Test-ConfigProperty -Config $smokeConfig -Name $Name) {
        return [int]$smokeConfig.$Name
    }

    return $Fallback
}

function Resolve-BoolSetting {
    param(
        [string]$Name,
        [switch]$CommandValue,
        [bool]$Fallback
    )

    if ($scriptBoundParameters.ContainsKey($Name)) {
        return $CommandValue.IsPresent
    }

    if (Test-ConfigProperty -Config $smokeConfig -Name $Name) {
        return [bool]$smokeConfig.$Name
    }

    return $Fallback
}

function Resolve-StringSetting {
    param(
        [string]$Name,
        [string]$CommandValue,
        [string]$Fallback
    )

    if ($scriptBoundParameters.ContainsKey($Name)) {
        return $CommandValue
    }

    if (Test-ConfigProperty -Config $smokeConfig -Name $Name) {
        return [string]$smokeConfig.$Name
    }

    return $Fallback
}

$SmokeFrames = Resolve-IntSetting -Name 'SmokeFrames' -CommandValue $SmokeFrames -Fallback 600
$SmokeCasePath = Resolve-StringSetting -Name 'SmokeCasePath' -CommandValue $SmokeCasePath -Fallback 'tools\et-battle-smoke.case.damage.json'
$MinBattleFrames = Resolve-IntSetting -Name 'MinBattleFrames' -CommandValue $MinBattleFrames -Fallback 30
$TimeoutMilliseconds = Resolve-IntSetting -Name 'TimeoutMilliseconds' -CommandValue $TimeoutMilliseconds -Fallback 15000
$SleepMilliseconds = Resolve-IntSetting -Name 'SleepMilliseconds' -CommandValue $SleepMilliseconds -Fallback 16
$DrainFrames = Resolve-IntSetting -Name 'DrainFrames' -CommandValue $DrainFrames -Fallback 5
$ConsistencyRuns = Resolve-IntSetting -Name 'ConsistencyRuns' -CommandValue $ConsistencyRuns -Fallback 2
$NoBuild = Resolve-BoolSetting -Name 'NoBuild' -CommandValue $NoBuild -Fallback $false
$SkipConfigValidation = Resolve-BoolSetting -Name 'SkipConfigValidation' -CommandValue $SkipConfigValidation -Fallback $false
$KeepOutput = Resolve-BoolSetting -Name 'KeepOutput' -CommandValue $KeepOutput -Fallback $false

if ([System.IO.Path]::IsPathRooted($SmokeCasePath)) {
    $resolvedSmokeCasePath = $SmokeCasePath
}
else {
    $resolvedSmokeCasePath = Join-Path $repoRoot $SmokeCasePath
}

if (-not (Test-Path $resolvedSmokeCasePath)) {
    throw "Smoke case file not found: $resolvedSmokeCasePath"
}

$project = Join-Path $repoRoot 'src\AbilityKit.Demo.ET.App\AbilityKit.Demo.ET.App.csproj'
$outputDirectory = Join-Path $repoRoot 'src\AbilityKit.Demo.ET.App'
$output = Join-Path $outputDirectory 'smoke-output.txt'

function Stop-SmokeProcesses {
    Get-CimInstance Win32_Process |
        Where-Object {
            $_.Name -eq 'dotnet.exe' -and
            $_.CommandLine -like '*AbilityKit.Demo.ET.App*--smoke*'
        } |
        ForEach-Object {
            Write-Host ("Stopping lingering smoke process {0}" -f $_.ProcessId) -ForegroundColor Yellow
            Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue
        }
}

function Remove-SmokeDiagnostics {
    $diagnosticFiles = @(
        'smoke-runtime-ascii.txt',
        'smoke-runtime-clean.txt',
        'smoke-runtime-output.txt',
        'src\AbilityKit.Demo.ET.App\smoke-output.txt',
        'src\AbilityKit.Demo.ET.App\smoke-output-run-*.txt'
    )

    foreach ($file in $diagnosticFiles) {
        $path = Join-Path $repoRoot $file
        foreach ($matchedPath in Get-ChildItem -Path $path -ErrorAction SilentlyContinue) {
            Remove-Item $matchedPath.FullName -Force
            Write-Host ("Removed diagnostic output {0}" -f $matchedPath.Name) -ForegroundColor DarkGray
        }
    }
}

function Invoke-ConfigValidation {
    $arguments = @(
        'run',
        '--no-build',
        '--project',
        $project,
        '--',
        '--validate-config-only'
    )

    Write-Host '=== Config Validation ===' -ForegroundColor Cyan
    & dotnet @arguments
    $validateExitCode = $LASTEXITCODE
    if ($validateExitCode -ne 0) {
        throw "Config validation failed with exit code $validateExitCode"
    }
}

function Invoke-SmokeRun {
    param(
        [int]$RunIndex
    )

    $runOutput = Join-Path $outputDirectory ("smoke-output-run-{0}.txt" -f $RunIndex)
    $arguments = @(
        'run',
        '--no-build',
        '--project',
        $project,
        '--',
        '--smoke',
        "--smoke-frames=$SmokeFrames",
        "--smoke-min-battle-frames=$MinBattleFrames",
        "--smoke-timeout-ms=$TimeoutMilliseconds",
        "--smoke-sleep-ms=$SleepMilliseconds",
        "--smoke-drain-frames=$DrainFrames",
        "--smoke-case=$resolvedSmokeCasePath"
    )

    Write-Host ("=== Smoke Run {0}/{1} ===" -f $RunIndex, $ConsistencyRuns) -ForegroundColor Cyan
    $null = & dotnet @arguments 2>&1 | Tee-Object -FilePath $runOutput
    $runExitCode = $LASTEXITCODE

    Stop-SmokeProcesses

    $passed = Select-String -Path $runOutput -Pattern '=== ET Battle Smoke Passed ===' -Quiet
    $resultLine = Select-String -Path $runOutput -Pattern '^\[ETBattleSmoke\]' | Select-Object -Last 1
    $signature = $null

    if ($resultLine -and $resultLine.Line -match 'DeterminismSignature=(.+)$') {
        $signature = $Matches[1]
    }

    [PSCustomObject]@{
        Index = $RunIndex
        Output = $runOutput
        ExitCode = $runExitCode
        Passed = ($runExitCode -eq 0 -and $passed)
        ResultLine = $(if ($resultLine) { $resultLine.Line } else { '' })
        Signature = $signature
    }
}

$ConsistencyRuns = [Math]::Max(1, $ConsistencyRuns)

Write-Host '=== AbilityKit ET Battle Smoke ===' -ForegroundColor Cyan
Write-Host ("Project: {0}" -f $project)
Write-Host ("Config: {0}" -f $resolvedConfigPath)
Write-Host ("SmokeCase: {0}" -f $resolvedSmokeCasePath)
Write-Host ("Frames: {0}, MinBattleFrames: {1}, TimeoutMs: {2}, SleepMs: {3}, DrainFrames: {4}, ConsistencyRuns: {5}, SkipConfigValidation: {6}" -f $SmokeFrames, $MinBattleFrames, $TimeoutMilliseconds, $SleepMilliseconds, $DrainFrames, $ConsistencyRuns, $SkipConfigValidation)

Stop-SmokeProcesses
Remove-SmokeDiagnostics

if (-not $NoBuild) {
    Write-Host '=== Build ===' -ForegroundColor Cyan
    dotnet build $project
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }
}

if (-not $SkipConfigValidation) {
    Invoke-ConfigValidation
}

$results = @()
for ($runIndex = 1; $runIndex -le $ConsistencyRuns; $runIndex++) {
    $results += Invoke-SmokeRun -RunIndex $runIndex
}

Write-Host '=== Smoke Summary ===' -ForegroundColor Cyan
foreach ($result in $results) {
    if ($result.ResultLine) {
        Write-Host ("Run {0}: {1}" -f $result.Index, $result.ResultLine)
    }
    else {
        Write-Host ("Run {0}: missing ETBattleSmoke result line" -f $result.Index) -ForegroundColor Yellow
    }
}

$failedRuns = @($results | Where-Object { -not $_.Passed -or [string]::IsNullOrWhiteSpace($_.Signature) })
if ($failedRuns.Count -gt 0) {
    foreach ($result in $failedRuns) {
        Write-Host ("Run {0} failed, exit code {1}, output kept at {2}" -f $result.Index, $result.ExitCode, $result.Output) -ForegroundColor Red
    }

    exit $(if ($failedRuns[0].ExitCode -ne 0) { $failedRuns[0].ExitCode } else { 2 })
}

$baselineSignature = $results[0].Signature
$mismatchedRuns = @($results | Where-Object { $_.Signature -ne $baselineSignature })
if ($mismatchedRuns.Count -gt 0) {
    Write-Host 'Consistency: Failed' -ForegroundColor Red
    foreach ($result in $results) {
        Write-Host ("Run {0} Signature: {1}" -f $result.Index, $result.Signature)
        Write-Host ("Run {0} Output: {1}" -f $result.Index, $result.Output) -ForegroundColor Yellow
    }

    exit 3
}

Write-Host 'Result: Passed' -ForegroundColor Green
Write-Host 'Consistency: Passed' -ForegroundColor Green
Write-Host ("DeterminismSignature: {0}" -f $baselineSignature)

if (-not $KeepOutput) {
    Remove-Item $output -Force -ErrorAction SilentlyContinue
    Get-ChildItem -Path (Join-Path $outputDirectory 'smoke-output-run-*.txt') -ErrorAction SilentlyContinue |
        ForEach-Object {
            Remove-Item $_.FullName -Force
            Write-Host ("Removed ET smoke output {0} after successful run" -f $_.Name) -ForegroundColor DarkGray
        }
}

exit 0
