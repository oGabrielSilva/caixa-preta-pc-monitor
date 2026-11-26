<#
.SYNOPSIS
    Builds the Caixa-Preta installer: artifacts/release/CaixaPreta-Setup-<version>.exe and its SHA-256.

.DESCRIPTION
    1. Publishes the collector, the dashboard (with the SPA) and the tray app, self-contained for win-x64.
    2. Stages them with the install scripts in artifacts/release/staging.
    3. Compiles installer/CaixaPreta.iss with Inno Setup 6 (ISCC.exe).
    Run it as a normal user, not elevated: publishing from an elevated shell leaves admin-owned build files.

.PARAMETER Version
    Product version; defaults to <Version> in Directory.Build.props.

.PARAMETER PnpmCommand
    How to launch pnpm for the SPA build ("corepack pnpm" locally, "pnpm" in CI).

.PARAMETER Iscc
    Path to ISCC.exe; searched in the usual Inno Setup 6 folders and on PATH when omitted.

.EXAMPLE
    powershell -File scripts/Build-Release.ps1
#>
[CmdletBinding()]
param(
    [string] $Version,
    [string] $PnpmCommand = 'corepack pnpm',
    [string] $Iscc
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$release = Join-Path $root 'artifacts\release'
$staging = Join-Path $release 'staging'

if (-not $Version) {
    $Version = ([xml] (Get-Content (Join-Path $root 'Directory.Build.props') -Raw)).Project.PropertyGroup.Version
}

if (-not $Iscc) {
    $candidates = @(
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
        (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe'),
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe')
    )
    $onPath = Get-Command 'ISCC.exe' -ErrorAction SilentlyContinue
    if ($onPath) { $candidates += $onPath.Source }
    $Iscc = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $Iscc) { throw 'Inno Setup 6 not found. Install it (winget install JRSoftware.InnoSetup) or pass -Iscc.' }
}

function Invoke-Checked([string] $Command, [string[]] $Arguments) {
    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Command failed (exit $LASTEXITCODE)." }
}

function Publish-Component([string] $Project, [string] $Folder, [string[]] $Extra = @()) {
    Write-Host "==> Publishing $Project" -ForegroundColor Cyan
    $arguments = @('publish', (Join-Path $root "src\$Project"), '-c', 'Release', '-r', 'win-x64', '--self-contained',
        '-o', (Join-Path $staging $Folder), "-p:Version=$Version") + $Extra
    Invoke-Checked 'dotnet' $arguments
}

if (Test-Path $staging) { Remove-Item -Path $staging -Recurse -Force }
New-Item -ItemType Directory -Path $staging -Force | Out-Null

Publish-Component 'PcMonitor.Collector' 'Collector'
Publish-Component 'PcMonitor.Dashboard' 'Dashboard' @("-p:PnpmCommand=$PnpmCommand")
Publish-Component 'PcMonitor.Tray' 'Tray'

Write-Host '==> Staging the install scripts' -ForegroundColor Cyan
$scripts = New-Item -ItemType Directory -Path (Join-Path $staging 'Scripts') -Force
Get-ChildItem -Path $PSScriptRoot -File | Where-Object { $_.Name -like 'Install-*.ps1' -or $_.Name -like 'Uninstall-*.ps1' -or $_.Name -eq 'PcMonitorService.psm1' } |
    Copy-Item -Destination $scripts.FullName

Write-Host "==> Compiling the installer with $Iscc" -ForegroundColor Cyan
Invoke-Checked $Iscc @('/Qp', "/DAppVersion=$Version", "/DStaging=$staging", "/O$release", (Join-Path $root 'installer\CaixaPreta.iss'))

$setup = Join-Path $release "CaixaPreta-Setup-$Version.exe"
$hash = (Get-FileHash -Path $setup -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -Path "$setup.sha256" -Value "$hash  $(Split-Path -Leaf $setup)" -Encoding ascii

Write-Host ''
Write-Host "Installer: $setup" -ForegroundColor Green
Write-Host "SHA-256:   $hash"
