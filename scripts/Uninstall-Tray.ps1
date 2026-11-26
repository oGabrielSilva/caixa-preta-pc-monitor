#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Removes the Caixa-Preta tray app: closes it, deletes its binaries, its Start menu shortcut and the current user's
    "start with Windows" entry. Other users' entries point to a missing file and Windows skips them.

.EXAMPLE
    powershell -File scripts/Uninstall-Tray.ps1
#>
[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'PcMonitorService.psm1') -Force

$installPath = Get-PcMonitorInstallPath -Component Tray
$shortcut = Join-Path (Get-PcMonitorStartMenuPath) 'Caixa-Preta.lnk'

if ($PSCmdlet.ShouldProcess('CaixaPreta.exe', 'Close the tray app')) {
    Stop-PcMonitorTray
}

if ((Test-Path $installPath) -and $PSCmdlet.ShouldProcess($installPath, 'Delete the tray app binaries')) {
    Remove-Item -Path $installPath -Recurse -Force
    Write-Host "Binaries removed from $installPath."
}

if ((Test-Path $shortcut) -and $PSCmdlet.ShouldProcess($shortcut, 'Delete the Start menu shortcut')) {
    Remove-Item -Path $shortcut -Force
    Write-Host 'Start menu shortcut removed.'
}

Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name 'CaixaPreta' -ErrorAction SilentlyContinue
