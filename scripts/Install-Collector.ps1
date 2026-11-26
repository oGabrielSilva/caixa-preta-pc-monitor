#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Installs (or updates) the Caixa-Preta collector as a Windows service.

.DESCRIPTION
    1. Ensures the PawnIO driver is installed (winget, ADR 0010).
    2. Copies the published collector to %ProgramFiles%\PcMonitor\Collector (unless -InPlace).
    3. Creates %ProgramData%\PcMonitor with an ACL limited to SYSTEM and Administrators.
    4. Registers the PcMonitor.Collector service (automatic start, LocalSystem, restart on failure) and starts it.
    The database is never touched: updating keeps all recorded data.

.PARAMETER SourcePath
    Folder produced by: dotnet publish src/PcMonitor.Collector -c Release -o artifacts/collector
    (publish as a normal user; publishing from an elevated shell leaves admin-owned build files in the repo).

.PARAMETER InPlace
    The binaries are already in the install folder (the Caixa-Preta installer copied them): use them, copy nothing.

.EXAMPLE
    powershell -File scripts/Install-Collector.ps1
#>
[CmdletBinding()]
param(
    [string] $SourcePath,
    [switch] $InPlace
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# $PSScriptRoot is empty in parameter defaults on Windows PowerShell 5.1, so defaults that need it are set here.
if (-not $SourcePath) { $SourcePath = Join-Path (Split-Path -Parent $PSScriptRoot) 'artifacts\collector' }

Import-Module (Join-Path $PSScriptRoot 'PcMonitorService.psm1') -Force

$serviceName = 'PcMonitor.Collector'
$displayName = 'Caixa-Preta (Coletor)'
$installPath = Get-PcMonitorInstallPath -Component Collector
$executable = Join-Path $installPath 'PcMonitor.Collector.exe'

Write-Step 'Checking the PawnIO driver'
if (Get-Service -Name 'PawnIO' -ErrorAction SilentlyContinue) {
    Write-Host '    PawnIO is installed.'
}
elseif (Get-Command winget -ErrorAction SilentlyContinue) {
    winget install --id namazso.PawnIO -e --accept-source-agreements --accept-package-agreements --disable-interactivity
    if ($LASTEXITCODE -ne 0) { Write-Warning "winget could not install PawnIO (exit $LASTEXITCODE). The collector will record 'DriverMissing'." }
}
else {
    Write-Warning "winget not found. Install PawnIO manually (https://pawnio.eu); until then the collector records 'DriverMissing'."
}

Write-Step "Stopping $serviceName (if installed)"
$existing = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
Stop-PcMonitorService -Name $serviceName

Install-PcMonitorBinaries -SourcePath $SourcePath -InstallPath $installPath -InPlace:$InPlace -RequiredFiles @('PcMonitor.Collector.exe') `
    -PublishHint 'Run as a normal user first: dotnet publish src/PcMonitor.Collector -c Release -o artifacts/collector'

Write-Step 'Preparing the data folder'
$dataPath = Initialize-PcMonitorDataFolder

Write-Step 'Registering the event log source'
Register-PcMonitorEventSource -Name $serviceName

Write-Step "Registering the $serviceName service"
if (-not $existing) {
    New-Service -Name $serviceName -BinaryPathName "`"$executable`"" -DisplayName $displayName `
        -Description 'Records hardware sensors every second and detects unexpected shutdowns.' -StartupType Automatic | Out-Null
}
Invoke-Sc @('config', $serviceName, 'DisplayName=', $displayName)
# Restart on failure: the stall watchdog relies on these actions to bring the collector back.
Set-PcMonitorRecovery -Name $serviceName

Write-Step "Starting $serviceName"
Start-Service -Name $serviceName
(Get-Service -Name $serviceName).WaitForStatus('Running', [TimeSpan]::FromSeconds(30))

Write-Host ''
Write-Host "Installed. Service: $((Get-Service -Name $serviceName).Status)" -ForegroundColor Green
Write-Host "Database: $(Join-Path $dataPath 'pcmonitor.db')"
