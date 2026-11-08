param([string]$Repository = 'bannerlord-berserk-black-swordsman')
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
foreach ($exe in @('git','gh')) {
    if (-not (Get-Command $exe -ErrorAction SilentlyContinue)) {
        throw "Missing '$exe'. Install Git for Windows and GitHub CLI (gh), then retry."
    }
}
& gh auth status
if ($LASTEXITCODE -ne 0) { throw 'Please run: gh auth login' }
if (Test-Path '.git') { throw 'Git repository already exists here. Review before publishing.' }
# PowerShell 5.1 with ErrorActionPreference=Stop turns gh's expected
# "repository not found" stderr into a terminating NativeCommandError.
# Temporarily allow native stderr, while preserving gh's actual exit code.
$previousErrorActionPreference = $ErrorActionPreference
try {
    $ErrorActionPreference = 'Continue'
    & gh repo view "medvedushechka/$Repository" --json name 2>$null | Out-Null
    $repoViewExitCode = $LASTEXITCODE
} finally {
    $ErrorActionPreference = $previousErrorActionPreference
}
if ($repoViewExitCode -eq 0) { throw "Repository already exists: medvedushechka/$Repository. Nothing changed." }
# If the failure was network/auth-related, gh repo create will report it explicitly.
Write-Host "Publishing NEW PUBLIC repository: medvedushechka/$Repository"
$confirm = Read-Host 'Type PUBLISH to continue'
if ($confirm -cne 'PUBLISH') { Write-Host 'Cancelled'; exit 0 }
& git init -b main
if ($LASTEXITCODE -ne 0) { throw 'git init failed' }
& git add .
if ($LASTEXITCODE -ne 0) { throw 'git add failed' }
& git commit -m 'Initial modpack manifest and UpgradeGuard v6 source'
if ($LASTEXITCODE -ne 0) { throw 'git commit failed. Configure git user identity and retry manually.' }
& gh repo create "medvedushechka/$Repository" --public --source . --remote origin --push
if ($LASTEXITCODE -ne 0) { throw 'GitHub publication failed. Check gh output; the local commit was retained.' }
Write-Host "Published: https://github.com/medvedushechka/$Repository"
