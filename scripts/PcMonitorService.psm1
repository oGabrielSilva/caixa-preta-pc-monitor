# Shared helpers for the PC Monitor install and uninstall scripts.

Set-StrictMode -Version Latest

$script:SystemSid = '*S-1-5-18'          # LocalSystem (language-independent)
$script:AdministratorsSid = '*S-1-5-32-544' # BUILTIN\Administrators (language-independent)

<# .SYNOPSIS Prints a step of an install script. #>
function Write-Step([string] $Message) { Write-Host "==> $Message" -ForegroundColor Cyan }

<# .SYNOPSIS The folder holding the database: %ProgramData%\PcMonitor. #>
function Get-PcMonitorDataPath { Join-Path $env:ProgramData 'PcMonitor' }

<# .SYNOPSIS Where a component's binaries are installed: %ProgramFiles%\PcMonitor\<Component>. #>
function Get-PcMonitorInstallPath([Parameter(Mandatory)] [ValidateSet('Collector', 'Dashboard')] [string] $Component) {
    Join-Path $env:ProgramFiles "PcMonitor\$Component"
}

<#
.SYNOPSIS
    Creates the data folder and limits it to SYSTEM and Administrators (full control), inheritance off.
.DESCRIPTION
    /grant:r replaces only the entries of those two accounts, so the dashboard's read entry survives a reinstall.
#>
function Initialize-PcMonitorDataFolder {
    $dataPath = Get-PcMonitorDataPath
    New-Item -ItemType Directory -Path $dataPath -Force | Out-Null
    icacls $dataPath /inheritance:r /grant:r "$($script:SystemSid):(OI)(CI)F" "$($script:AdministratorsSid):(OI)(CI)F" | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "icacls failed on $dataPath (exit $LASTEXITCODE)." }
    $dataPath
}

<#
.SYNOPSIS
    Stops a service and waits until its process has exited.
.DESCRIPTION
    The service control manager reports Stopped before the process has exited and released its files, so copying or
    deleting binaries right after Stop-Service can fail on locked DLLs.
#>
function Stop-PcMonitorService([Parameter(Mandatory)] [string] $Name) {
    $service = Get-Service -Name $Name -ErrorAction SilentlyContinue
    if (-not $service -or $service.Status -eq 'Stopped') { return }

    $processId = (Get-CimInstance Win32_Service -Filter "Name='$Name'").ProcessId
    Stop-Service -Name $Name
    $service.WaitForStatus('Stopped', [TimeSpan]::FromSeconds(30))
    if ($processId) { Wait-Process -Id $processId -Timeout 30 -ErrorAction SilentlyContinue }
}

<# .SYNOPSIS Runs sc.exe and throws when it fails. #>
function Invoke-Sc([string[]] $Arguments) {
    sc.exe @Arguments | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "sc.exe $($Arguments -join ' ') failed (exit $LASTEXITCODE)." }
}

<# .SYNOPSIS The SID of a service's virtual account (NT SERVICE\<Name>), computed from the name. #>
function Get-PcMonitorServiceSid([Parameter(Mandatory)] [string] $Name) {
    $output = sc.exe showsid $Name
    if ($LASTEXITCODE -ne 0) { throw "sc.exe showsid $Name failed (exit $LASTEXITCODE)." }
    $sid = $output | Select-String -Pattern 'S-1-5-80-[\d-]+' | ForEach-Object { $_.Matches[0].Value } | Select-Object -First 1
    if (-not $sid) { throw "No service SID found for $Name." }
    $sid
}

<# .SYNOPSIS Registers an Application event log source (needs administrator; a service account cannot create it). #>
function Register-PcMonitorEventSource([Parameter(Mandatory)] [string] $Name) {
    if (-not [System.Diagnostics.EventLog]::SourceExists($Name)) {
        [System.Diagnostics.EventLog]::CreateEventSource($Name, 'Application')
    }
}

<# .SYNOPSIS Restart on failure: 5 s, 5 s, then 30 s; the counter resets after a day. #>
function Set-PcMonitorRecovery([Parameter(Mandatory)] [string] $Name) {
    Invoke-Sc @('failure', $Name, 'reset=', '86400', 'actions=', 'restart/5000/restart/5000/restart/30000')
    Invoke-Sc @('failureflag', $Name, '1')
}

Export-ModuleMember -Function Write-Step, Get-PcMonitorDataPath, Get-PcMonitorInstallPath, Initialize-PcMonitorDataFolder,
    Stop-PcMonitorService, Invoke-Sc, Get-PcMonitorServiceSid, Register-PcMonitorEventSource, Set-PcMonitorRecovery
