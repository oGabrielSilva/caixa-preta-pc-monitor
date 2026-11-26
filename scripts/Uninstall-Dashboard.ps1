#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Removes the Caixa-Preta dashboard: service, binaries, Start menu shortcut and its read access to the data folder.
    Recorded data is never touched.

.EXAMPLE
    powershell -File scripts/Uninstall-Dashboard.ps1
#>
[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'PcMonitorService.psm1') -Force

$serviceName = 'PcMonitor.Dashboard'
$installPath = Get-PcMonitorInstallPath -Component Dashboard
$dataPath = Get-PcMonitorDataPath
$shortcut = Join-Path (Get-PcMonitorStartMenuPath) 'Caixa-Preta.url'

$service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($service -and $PSCmdlet.ShouldProcess($serviceName, 'Stop and delete the service')) {
    Stop-PcMonitorService -Name $serviceName
    Invoke-Sc @('delete', $serviceName)
    Write-Host "Service $serviceName removed."
}

if ((Test-Path $dataPath) -and $PSCmdlet.ShouldProcess($dataPath, "Remove the read access of NT SERVICE\$serviceName")) {
    $sid = Get-PcMonitorServiceSid -Name $serviceName
    icacls $dataPath /remove "*$sid" | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "icacls failed on $dataPath (exit $LASTEXITCODE)." }
    Write-Host "Read access removed from $dataPath (data kept)."
}

if ((Test-Path $installPath) -and $PSCmdlet.ShouldProcess($installPath, 'Delete the dashboard binaries')) {
    Remove-Item -Path $installPath -Recurse -Force
    Write-Host "Binaries removed from $installPath."
}

if ((Test-Path $shortcut) -and $PSCmdlet.ShouldProcess($shortcut, 'Delete the Start menu shortcut')) {
    Remove-Item -Path $shortcut -Force
    Write-Host 'Start menu shortcut removed.'
}
