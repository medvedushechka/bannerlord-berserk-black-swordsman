# Bannerlord UpgradeGuard v6 installer: diagnostic and temporary workaround for
# CharacterObject.GetUpgradeGoldCost invalid upgrade index, UI and roster diagnostics.
# Targeted at the exact CampaignSystem DLL hash supplied by the user.
# Creates only its own module. Does not modify existing DLLs, saves or XML.
param([switch]$Remove)

$ErrorActionPreference = 'Stop'
$game = 'C:\Games\Mount and Blade II Bannerlord'
$moduleDir = Join-Path $game 'Modules\BannerlordUpgradeGuard'
$moduleDll = Join-Path $moduleDir 'bin\Win64_Shipping_Client\BannerlordUpgradeGuard.dll'
$moduleXml = Join-Path $moduleDir 'SubModule.xml'
$expectedCampaignSha = '1F8E33E2ED73E6EC653D7629180AFB70649DDC6E5BD1657A802A264EFDA1C3AE'
$logDir = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'BannerlordUpgradeGuard'

$gameProcesses = @(Get-Process -ErrorAction SilentlyContinue | Where-Object { $_.ProcessName -match 'Bannerlord|TaleWorlds' })
if ($gameProcesses.Count -gt 0) { throw 'Close Bannerlord before installing or removing the guard.' }

if ($Remove) {
    if (-not (Test-Path -LiteralPath $moduleDir)) {
        Write-Host 'UpgradeGuard is not installed.' -ForegroundColor Yellow
        exit 0
    }
    if (-not (Test-Path -LiteralPath $moduleXml)) {
        throw 'UpgradeGuard folder exists without its SubModule.xml. Refusing to delete unknown files.'
    }
    $xmlText = [System.IO.File]::ReadAllText($moduleXml)
    if (-not $xmlText.Contains('<Id value="BannerlordUpgradeGuard"')) {
        throw 'Unexpected SubModule.xml. Refusing to remove unknown module.'
    }
    Remove-Item -LiteralPath $moduleDir -Recurse -Force
    Write-Host 'UpgradeGuard module removed. Saves and other mods untouched.' -ForegroundColor Green
    exit 0
}

$bin = Join-Path $game 'bin\Win64_Shipping_Client'
$campaign = Join-Path $bin 'TaleWorlds.CampaignSystem.dll'
$mountAndBlade = Join-Path $bin 'TaleWorlds.MountAndBlade.dll'
foreach ($file in @($campaign, $mountAndBlade)) {
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { throw "Missing game DLL: $file" }
}
$sha = (Get-FileHash -LiteralPath $campaign -Algorithm SHA256).Hash.ToUpperInvariant()
if ($sha -ne $expectedCampaignSha) {
    throw "Unsupported CampaignSystem DLL (SHA256 $sha). Nothing installed."
}
# Upgrade only our own existing module, never an unrelated mod.
$updating = Test-Path -LiteralPath $moduleDir
if ($updating) {
    if (-not (Test-Path -LiteralPath $moduleXml -PathType Leaf)) {
        throw "Existing module has no SubModule.xml: $moduleDir. Refusing to overwrite."
    }
    $existingXml = [System.IO.File]::ReadAllText($moduleXml)
    if (-not $existingXml.Contains('<Id value="BannerlordUpgradeGuard"')) {
        throw "Existing module has an unexpected ID. Refusing to overwrite."
    }
    if (-not (Test-Path -LiteralPath $moduleDll -PathType Leaf)) {
        throw "Existing UpgradeGuard DLL missing: $moduleDll. Refusing to overwrite."
    }
}

$harmonyCandidates = @(
    (Join-Path $game 'Modules\Bannerlord.Harmony\bin\Win64_Shipping_Client\0Harmony.dll'),
    (Join-Path $game 'Modules\BetterBanditsPlus\bin\Win64_Shipping_Client\0Harmony.dll')
)
$harmonyDll = $harmonyCandidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
if (-not $harmonyDll) {
    throw 'Cannot find 0Harmony.dll. Verify Bannerlord.Harmony installation.'
}

$sourceFile = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) 'UpgradeGuard.cs'
if (-not (Test-Path -LiteralPath $sourceFile -PathType Leaf)) { throw "Missing C# source: $sourceFile" }
$source = [System.IO.File]::ReadAllText($sourceFile)

$subModule = @'
<?xml version="1.0" encoding="utf-8"?>
<Module>
  <Name value="Bannerlord UpgradeGuard (diagnostic)" />
  <Id value="BannerlordUpgradeGuard" />
  <Version value="v1.0.4" />
  <SingleplayerModule value="true" />
  <MultiplayerModule value="false" />
  <DependedModules>
    <DependedModule Id="Native" />
    <DependedModule Id="SandBoxCore" />
    <DependedModule Id="Sandbox" />
    <DependedModule Id="Bannerlord.Harmony" />
  </DependedModules>
  <SubModules>
    <SubModule>
      <Name value="Bannerlord UpgradeGuard" />
      <DLLName value="BannerlordUpgradeGuard.dll" />
      <SubModuleClassType value="BannerlordUpgradeGuard.SubModule" />
      <Tags />
    </SubModule>
  </SubModules>
</Module>
'@

$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ('BannerlordUpgradeGuardBuild_' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
try {
    $stagedDll = Join-Path $tempDir 'BannerlordUpgradeGuard.dll'
    Write-Host 'Compiling the diagnostic module with Windows PowerShell...' -ForegroundColor Cyan
    $references = @($mountAndBlade, $harmonyDll)
    foreach ($name in @('TaleWorlds.CampaignSystem.dll', 'TaleWorlds.Core.dll', 'TaleWorlds.Library.dll', 'TaleWorlds.ObjectSystem.dll', 'TaleWorlds.CampaignSystem.ViewModelCollection.dll', 'TaleWorlds.Localization.dll', 'TaleWorlds.ModuleManager.dll')) {
        $candidate = Join-Path $bin $name
        if (Test-Path -LiteralPath $candidate -PathType Leaf) { $references += $candidate }
    }

    # PowerShell 5.1 Add-Type uses the .NET Framework compiler. Game assemblies
    # reference the netstandard 2.0 facade, which Add-Type does not auto-resolve.
    $netstandardCandidates = New-Object 'System.Collections.Generic.List[string]'
    $netstandardCandidates.Add((Join-Path $bin 'netstandard.dll'))
    foreach ($frameworkRoot in @(
        (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\Facades'),
        (Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\Facades')
    )) {
        $netstandardCandidates.Add((Join-Path $frameworkRoot 'netstandard.dll'))
    }
    foreach ($pf in @(${env:ProgramFiles(x86)}, $env:ProgramFiles)) {
        if (-not $pf) { continue }
        foreach ($ver in @('v4.8.1', 'v4.8', 'v4.7.2', 'v4.7.1', 'v4.7')) {
            $netstandardCandidates.Add((Join-Path $pf "Reference Assemblies\Microsoft\Framework\.NETFramework\$ver\Facades\netstandard.dll"))
        }
    }
    $gacRoot = Join-Path $env:WINDIR 'Microsoft.NET\assembly\GAC_MSIL\netstandard'
    if (Test-Path -LiteralPath $gacRoot -PathType Container) {
        Get-ChildItem -LiteralPath $gacRoot -Filter 'netstandard.dll' -File -Recurse -ErrorAction SilentlyContinue |
            ForEach-Object { $netstandardCandidates.Add($_.FullName) }
    }
    # Keep only the exact facade assembly version required by the game.
    $netstandardDll = $null
    foreach ($candidate in $netstandardCandidates) {
        if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) { continue }
        try {
            $asmName = [System.Reflection.AssemblyName]::GetAssemblyName($candidate)
            if ($asmName.Name -eq 'netstandard' -and $asmName.Version.Major -eq 2 -and $asmName.Version.Minor -eq 0) {
                $netstandardDll = $candidate
                break
            }
        }
        catch { continue }
    }
    if (-not $netstandardDll) {
        throw 'Cannot locate netstandard.dll v2.0.0.0. Install the Microsoft .NET Framework 4.8 Developer Pack, then rerun this installer. Nothing was installed.'
    }
    Write-Host "Using netstandard reference: $netstandardDll" -ForegroundColor DarkCyan
    $references += $netstandardDll
    $references = @($references | Select-Object -Unique)
    Add-Type -TypeDefinition $source -OutputAssembly $stagedDll -ReferencedAssemblies $references -ErrorAction Stop
    if (-not (Test-Path -LiteralPath $stagedDll -PathType Leaf)) { throw 'C# compilation produced no DLL.' }
    $targetBin = Join-Path $moduleDir 'bin\Win64_Shipping_Client'
    # Build completed successfully. Preserve the existing diagnostic module for rollback.
    $backupDll = Join-Path $tempDir 'previous_BannerlordUpgradeGuard.dll'
    $backupXml = Join-Path $tempDir 'previous_SubModule.xml'
    if ($updating) {
        Copy-Item -LiteralPath $moduleDll -Destination $backupDll -ErrorAction Stop
        Copy-Item -LiteralPath $moduleXml -Destination $backupXml -ErrorAction Stop
    }
    try {
        New-Item -ItemType Directory -Path $targetBin -Force | Out-Null
        Copy-Item -LiteralPath $stagedDll -Destination $moduleDll -Force -ErrorAction Stop
        [System.IO.File]::WriteAllText($moduleXml, $subModule, [System.Text.Encoding]::UTF8)
    }
    catch {
        if ($updating) {
            Copy-Item -LiteralPath $backupDll -Destination $moduleDll -Force -ErrorAction SilentlyContinue
            Copy-Item -LiteralPath $backupXml -Destination $moduleXml -Force -ErrorAction SilentlyContinue
        }
        else {
            if (Test-Path -LiteralPath $moduleDir) { Remove-Item -LiteralPath $moduleDir -Recurse -Force }
        }
        throw
    }
    Write-Host 'UpgradeGuard v6 installed successfully.' -ForegroundColor Green
    Write-Host 'Enable "Bannerlord UpgradeGuard (diagnostic)" in the game launcher, after Harmony.' -ForegroundColor Yellow
    Write-Host "Runtime log: $(Join-Path $logDir 'upgrade_guard.log')"
    Write-Host 'To remove: run this script with -Remove.'
    Write-Host 'No saves, game DLLs, or other mods were modified.'
}
catch {
    # Never remove a previously installed module on compilation failure.
    throw
}
finally {
    if (Test-Path -LiteralPath $tempDir) { Remove-Item -LiteralPath $tempDir -Recurse -Force }
}
