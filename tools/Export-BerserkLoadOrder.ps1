# Export-BerserkLoadOrder.ps1
# Read-only. Produces sanitized module-order CSV + JSON in a ZIP on the Desktop.
# Does not read or export Vortex state.v2, credentials or save files.
[CmdletBinding()]
param(
    [string]$GameRoot = 'C:\Games\Mount and Blade II Bannerlord',
    [string]$OutputDir = [Environment]::GetFolderPath('Desktop')
)
$ErrorActionPreference = 'Stop'
$docs = @([Environment]::GetFolderPath('MyDocuments'),
          "$env:USERPROFILE\OneDrive\Документы",
          "$env:USERPROFILE\OneDrive\Documents",
          "$env:USERPROFILE\Documents") | Select-Object -Unique
$candidates = foreach ($d in $docs) {
    if (-not [string]::IsNullOrWhiteSpace($d)) {
        Join-Path $d 'Mount and Blade II Bannerlord\Configs\LauncherData.xml'
        Join-Path $d 'Mount and Blade II Bannerlord\LauncherData.xml'
    }
}
$found = @($candidates | Where-Object { Test-Path -LiteralPath $_ } | ForEach-Object { Get-Item -LiteralPath $_ })
if ($found.Count -eq 0) {
    throw 'LauncherData.xml not found. Open the Bannerlord launcher once, close it, then retry. No files changed.'
}
$source = $found | Sort-Object LastWriteTime -Descending | Select-Object -First 1
[xml]$xml = Get-Content -LiteralPath $source.FullName -Raw -Encoding UTF8
$nodes = @($xml.SelectNodes('//SingleplayerData/ModDatas/UserModData'))
if ($nodes.Count -eq 0) { throw 'No singleplayer modules found in LauncherData.xml.' }
$records = @()
$index = 0
foreach ($node in $nodes) {
    $index++
    $id = [string]$node.Id
    $selected = ([string]$node.IsSelected).Trim().ToLowerInvariant() -eq 'true'
    $ver = ''
    $moduleFolder = ''
    foreach ($folder in @(Get-ChildItem -LiteralPath (Join-Path $GameRoot 'Modules') -Directory -ErrorAction SilentlyContinue)) {
        $sm = Join-Path $folder.FullName 'SubModule.xml'
        if (-not (Test-Path -LiteralPath $sm)) { continue }
        try {
            [xml]$m = Get-Content -LiteralPath $sm -Raw -Encoding UTF8
            if ([string]$m.Module.Id.value -eq $id) {
                $ver = [string]$m.Module.Version.value
                $moduleFolder = $folder.Name
                break
            }
        } catch { }
    }
    $records += [pscustomobject]@{
        Order = $index; ModuleId = $id; Selected = $selected
        InstalledVersion = $ver; ModuleFolder = $moduleFolder
    }
}
$stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$folder = Join-Path $env:TEMP "Berserk_Current_LoadOrder_$stamp"
New-Item -ItemType Directory -Path $folder -Force | Out-Null
try {
    $records | Export-Csv -LiteralPath (Join-Path $folder 'current_load_order.csv') -NoTypeInformation -Encoding UTF8
    [pscustomobject]@{
        SourceLastWriteTimeLocal = $source.LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss')
        ExportTimeLocal = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')
        TotalModules = $records.Count
        EnabledModules = @($records | Where-Object Selected).Count
        Note = 'Launcher order only; not a Vortex collection or a Vortex profile export.'
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $folder 'summary.json') -Encoding UTF8
    if (-not (Test-Path -LiteralPath $OutputDir)) { New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null }
    $zip = Join-Path $OutputDir "Berserk_Current_LoadOrder_$stamp.zip"
    Compress-Archive -Path (Join-Path $folder '*') -DestinationPath $zip -Force
    Write-Host "Created: $zip"
    Write-Host "Modules: $($records.Count); enabled: $(@($records | Where-Object Selected).Count)"
    Write-Host "LauncherData last modified: $($source.LastWriteTime)"
    Write-Host 'No game, Vortex or save files were modified.'
} finally {
    Remove-Item -LiteralPath $folder -Recurse -Force -ErrorAction SilentlyContinue
}
