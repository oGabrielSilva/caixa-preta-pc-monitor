#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Installs (or updates) the Caixa-Preta tray app for every user of the computer.

.DESCRIPTION
    1. Closes the running tray app (in every session) so its files can be replaced.
    2. Copies the published app to %ProgramFiles%\PcMonitor\Tray (unless -InPlace).
    3. Adds a "Caixa-Preta" shortcut to CaixaPreta.exe in the Start menu of every user; it replaces the dashboard's
       browser shortcut, since the app shows the dashboard in its own window.
    "Start with Windows" is not set here: the app turns it on for each user at its first run, and the user can turn it
    off in the tray menu.

.PARAMETER SourcePath
    Folder produced by (as a normal user): dotnet publish src/PcMonitor.Tray -c Release -o artifacts/tray

.PARAMETER InPlace
    The binaries are already in the install folder (the Caixa-Preta installer copied them): use them, copy nothing.

.EXAMPLE
    powershell -File scripts/Install-Tray.ps1
#>
[CmdletBinding()]
param(
    [string] $SourcePath,
    [switch] $InPlace
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# $PSScriptRoot is empty in parameter defaults on Windows PowerShell 5.1, so defaults that need it are set here.
if (-not $SourcePath) { $SourcePath = Join-Path (Split-Path -Parent $PSScriptRoot) 'artifacts\tray' }

Import-Module (Join-Path $PSScriptRoot 'PcMonitorService.psm1') -Force

$installPath = Get-PcMonitorInstallPath -Component Tray
$executable = Join-Path $installPath 'CaixaPreta.exe'
$startMenu = Get-PcMonitorStartMenuPath

if (-not $InPlace) {
    Write-Step 'Closing the tray app (if running)'
    Stop-PcMonitorTray
}

Install-PcMonitorBinaries -SourcePath $SourcePath -InstallPath $installPath -InPlace:$InPlace -RequiredFiles @('CaixaPreta.exe') `
    -PublishHint 'Run as a normal user first: dotnet publish src/PcMonitor.Tray -c Release -o artifacts/tray'

Write-Step 'Adding the Start menu shortcut'
$link = (New-Object -ComObject WScript.Shell).CreateShortcut((Join-Path $startMenu 'Caixa-Preta.lnk'))
$link.TargetPath = $executable
$link.WorkingDirectory = $installPath
$link.Description = 'Caixa-Preta: hardware monitor'
$link.Save()
Remove-Item -Path (Join-Path $startMenu 'Caixa-Preta.url') -ErrorAction SilentlyContinue

Write-Host ''
Write-Host "Installed: $executable" -ForegroundColor Green
