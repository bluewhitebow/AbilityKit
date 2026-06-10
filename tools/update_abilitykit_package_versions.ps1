$ErrorActionPreference = 'Stop'

$packageDirs = Get-ChildItem -Path 'Unity\Packages' -Directory -Filter 'com.abilitykit*'
foreach ($dir in $packageDirs) {
    $packagePath = Join-Path $dir.FullName 'package.json'
    if (-not (Test-Path -LiteralPath $packagePath)) {
        continue
    }

    $text = Get-Content -LiteralPath $packagePath -Raw
    if ($text -notmatch '"name"\s*:\s*"com\.abilitykit\.') {
        continue
    }

    $newText = $text
    $newText = $newText -replace '("version"\s*:\s*")(?:1\.0\.0|0\.1\.0)(")', '${1}0.0.1${2}'
    $newText = $newText -replace '("com\.abilitykit\.[^"]+"\s*:\s*")(?:1\.0\.0|0\.1\.0)(")', '${1}0.0.1${2}'

    if ($newText -ne $text) {
        Set-Content -LiteralPath $packagePath -Value $newText -NoNewline -Encoding UTF8
    }
}

$lockPath = 'Unity\Packages\packages-lock.json'
if (Test-Path -LiteralPath $lockPath) {
    $text = Get-Content -LiteralPath $lockPath -Raw
    $newText = $text -replace '("com\.abilitykit\.[^"]+"\s*:\s*")(?:1\.0\.0|0\.1\.0)(")', '${1}0.0.1${2}'
    if ($newText -ne $text) {
        Set-Content -LiteralPath $lockPath -Value $newText -NoNewline -Encoding UTF8
    }
}
