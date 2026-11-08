# Collect UpgradeGuard logs and the newest Bannerlord crash logs into a ZIP.
# Use -IncludeDump to include dump.dmp (may be large).
param([switch]$IncludeDump)
$ErrorActionPreference = 'Stop'
$sourceLog = Join-Path $env:LOCALAPPDATA 'BannerlordUpgradeGuard'
$crashRoot = Join-Path $env:ProgramData 'Mount and Blade II Bannerlord\crashes'
$desktop = [Environment]::GetFolderPath('Desktop')
$stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$output = Join-Path $desktop "Bannerlord_UpgradeGuard_Logs_$stamp.zip"
$temp = Join-Path $env:TEMP ("UpgradeGuardLogs_" + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temp -Force | Out-Null
try {
    if (Test-Path -LiteralPath $sourceLog -PathType Container) {
        $dst = Join-Path $temp 'UpgradeGuard'
        New-Item -ItemType Directory -Path $dst -Force | Out-Null
        foreach ($name in @('upgrade_guard.log', 'upgrade_guard.log.previous', 'session.open')) {
            $src = Join-Path $sourceLog $name
            if (Test-Path -LiteralPath $src -PathType Leaf) {
                Copy-Item -LiteralPath $src -Destination (Join-Path $dst $name) -Force
            }
        }
    }
    if (Test-Path -LiteralPath $crashRoot -PathType Container) {
        $latest = Get-ChildItem -LiteralPath $crashRoot -Directory -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($latest) {
            $dst = Join-Path $temp 'LatestCrash'
            New-Item -ItemType Directory -Path $dst -Force | Out-Null
            Get-ChildItem -LiteralPath $latest.FullName -File -Recurse -ErrorAction SilentlyContinue |
                Where-Object { $_.Extension -in @('.txt', '.log', '.xml', '.json') -or ($IncludeDump -and $_.Name -eq 'dump.dmp') } |
                ForEach-Object {
                    $relative = $_.FullName.Substring($latest.FullName.Length).TrimStart('\')
                    $target = Join-Path $dst $relative
                    New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
                    Copy-Item -LiteralPath $_.FullName -Destination $target -Force
                }
        }
    }
    $files = @(Get-ChildItem -LiteralPath $temp -File -Recurse -ErrorAction SilentlyContinue)
    if ($files.Count -eq 0) { throw 'No logs found; start the game with UpgradeGuard enabled first.' }
    Compress-Archive -Path (Join-Path $temp '*') -DestinationPath $output -Force
    Write-Host "Archive created: $output" -ForegroundColor Green
    Write-Host "Files: $($files.Count); dump included: $IncludeDump"
}
finally { if (Test-Path -LiteralPath $temp) { Remove-Item -LiteralPath $temp -Recurse -Force } }
