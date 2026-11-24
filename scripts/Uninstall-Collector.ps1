#Requires -Version 7
#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Removes the PC Monitor collector service and binaries. Recorded data is kept unless -RemoveData is given.

.EXAMPLE
    pwsh scripts/Uninstall-Collector.ps1
    pwsh scripts/Uninstall-Collector.ps1 -RemoveData
#>
[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param(
    [switch] $RemoveData
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'PcMonitorService.psm1') -Force

$serviceName = 'PcMonitor.Collector'
$installPath = Get-PcMonitorInstallPath -Component Collector
$dataPath = Get-PcMonitorDataPath

$service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($service -and $PSCmdlet.ShouldProcess($serviceName, 'Stop and delete the service')) {
    Stop-PcMonitorService -Name $serviceName
    Invoke-Sc @('delete', $serviceName)
    Write-Host "Service $serviceName removed."
}

if ((Test-Path $installPath) -and $PSCmdlet.ShouldProcess($installPath, 'Delete the collector binaries')) {
    Remove-Item -Path $installPath -Recurse -Force
    Write-Host "Binaries removed from $installPath."
}

if ($RemoveData -and (Test-Path $dataPath) -and $PSCmdlet.ShouldProcess($dataPath, 'Delete all recorded monitoring data (cannot be undone)')) {
    Remove-Item -Path $dataPath -Recurse -Force
    Write-Host "Data removed from $dataPath."
}
elseif (Test-Path $dataPath) {
    Write-Host "Data kept in $dataPath."
}
