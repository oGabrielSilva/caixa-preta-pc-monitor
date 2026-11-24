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

$serviceName = 'PcMonitor.Collector'
$installPath = Join-Path $env:ProgramFiles 'PcMonitor\Collector'
$dataPath = Join-Path $env:ProgramData 'PcMonitor'

$service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($service) {
    if ($service.Status -ne 'Stopped') {
        Stop-Service -Name $serviceName
        $service.WaitForStatus('Stopped', [TimeSpan]::FromSeconds(30))
    }
    sc.exe delete $serviceName | Out-Null
    Write-Host "Service $serviceName removed."
}

if (Test-Path $installPath) {
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
