# Shared helpers for the collector install and uninstall scripts.

Set-StrictMode -Version Latest

$script:ServiceName = 'PcMonitor.Collector'

function Get-CollectorServiceName { $script:ServiceName }

function Get-CollectorInstallPath { Join-Path $env:ProgramFiles 'PcMonitor\Collector' }

function Get-CollectorDataPath { Join-Path $env:ProgramData 'PcMonitor' }

<#
.SYNOPSIS
    Stops the collector service and waits until its process has exited.
.DESCRIPTION
    The service control manager reports Stopped before the process has exited and released its files, so copying or
    deleting binaries right after Stop-Service can fail on locked DLLs.
#>
function Stop-CollectorService {
    $service = Get-Service -Name $script:ServiceName -ErrorAction SilentlyContinue
    if (-not $service -or $service.Status -eq 'Stopped') { return }

    $processId = (Get-CimInstance Win32_Service -Filter "Name='$($script:ServiceName)'").ProcessId
    Stop-Service -Name $script:ServiceName
    $service.WaitForStatus('Stopped', [TimeSpan]::FromSeconds(30))
    if ($processId) { Wait-Process -Id $processId -Timeout 30 -ErrorAction SilentlyContinue }
}

<# .SYNOPSIS Runs sc.exe and throws when it fails. #>
function Invoke-Sc([string[]] $Arguments) {
    sc.exe @Arguments | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "sc.exe $($Arguments -join ' ') failed (exit $LASTEXITCODE)." }
}

Export-ModuleMember -Function Get-CollectorServiceName, Get-CollectorInstallPath, Get-CollectorDataPath, Stop-CollectorService, Invoke-Sc
