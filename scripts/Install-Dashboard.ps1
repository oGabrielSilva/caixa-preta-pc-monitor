#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Installs (or updates) the Caixa-Preta dashboard as a Windows service without privileges.

.DESCRIPTION
    1. Copies the published dashboard to %ProgramFiles%\PcMonitor\Dashboard (unless -InPlace).
    2. Registers the PcMonitor.Dashboard service under its virtual account NT SERVICE\PcMonitor.Dashboard
       (automatic start, restart on failure) and its event log source.
    3. Gives that account read access to %ProgramData%\PcMonitor (the collector's database); nothing else.
    4. Adds a "Caixa-Preta" shortcut to http://127.0.0.1:5280 in the Start menu of every user, unless the tray app
       is installed (its shortcut opens the dashboard in its own window).
    5. Starts the service and prints whether it can read the database.
    The database is never touched.

.PARAMETER SourcePath
    Folder produced by (as a normal user, not elevated):
    dotnet publish src/PcMonitor.Dashboard -c Release -o artifacts/dashboard -p:PnpmCommand="corepack pnpm"

.PARAMETER InPlace
    The binaries are already in the install folder (the Caixa-Preta installer copied them): use them, copy nothing.

.EXAMPLE
    powershell -File scripts/Install-Dashboard.ps1
#>
[CmdletBinding()]
param(
    [string] $SourcePath,
    [switch] $InPlace
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# $PSScriptRoot is empty in parameter defaults on Windows PowerShell 5.1, so defaults that need it are set here.
if (-not $SourcePath) { $SourcePath = Join-Path (Split-Path -Parent $PSScriptRoot) 'artifacts\dashboard' }

Import-Module (Join-Path $PSScriptRoot 'PcMonitorService.psm1') -Force

$serviceName = 'PcMonitor.Dashboard'
$displayName = 'Caixa-Preta (Painel)'
$account = "NT SERVICE\$serviceName"
$installPath = Get-PcMonitorInstallPath -Component Dashboard
$executable = Join-Path $installPath 'PcMonitor.Dashboard.exe'
$url = 'http://127.0.0.1:5280/'
$startMenu = Get-PcMonitorStartMenuPath
$shortcut = Join-Path $startMenu 'Caixa-Preta.url'

Write-Step "Stopping $serviceName (if installed)"
$existing = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
Stop-PcMonitorService -Name $serviceName

Install-PcMonitorBinaries -SourcePath $SourcePath -InstallPath $installPath -InPlace:$InPlace -RequiredFiles @('PcMonitor.Dashboard.exe', 'wwwroot\index.html') `
    -PublishHint 'Run as a normal user first: dotnet publish src/PcMonitor.Dashboard -c Release -o artifacts/dashboard -p:PnpmCommand="corepack pnpm"'

Write-Step 'Registering the event log source'
Register-PcMonitorEventSource -Name $serviceName

Write-Step "Registering the $serviceName service as $account"
if (-not $existing) {
    New-Service -Name $serviceName -BinaryPathName "`"$executable`"" -DisplayName $displayName `
        -Description 'Serves the Caixa-Preta dashboard on http://127.0.0.1:5280 (read-only, local only).' -StartupType Automatic | Out-Null
}
# A virtual account needs the service SID enabled; the account is the service's own and has no rights by default.
Invoke-Sc @('config', $serviceName, 'DisplayName=', $displayName)
Invoke-Sc @('sidtype', $serviceName, 'unrestricted')
Invoke-Sc @('config', $serviceName, 'obj=', $account)
Set-PcMonitorRecovery -Name $serviceName

Write-Step "Granting $account read access to the data folder"
$dataPath = Get-PcMonitorDataPath
if (-not (Test-Path $dataPath)) { $dataPath = Initialize-PcMonitorDataFolder }
$sid = Get-PcMonitorServiceSid -Name $serviceName
icacls $dataPath /grant "*${sid}:(OI)(CI)RX" | Out-Null
if ($LASTEXITCODE -ne 0) { throw "icacls failed on $dataPath (exit $LASTEXITCODE)." }

# Before the product had a name the shortcut was "PC Monitor".
Remove-Item -Path (Join-Path $startMenu 'PC Monitor.url') -ErrorAction SilentlyContinue
if (Test-Path (Join-Path $startMenu 'Caixa-Preta.lnk')) {
    Write-Step 'Start menu: the tray app already has the Caixa-Preta shortcut'
}
else {
    Write-Step 'Adding the Start menu shortcut'
    Set-Content -Path $shortcut -Value "[InternetShortcut]`r`nURL=$url`r`nIconIndex=0" -Encoding ascii
}

Write-Step "Starting $serviceName"
Start-Service -Name $serviceName
(Get-Service -Name $serviceName).WaitForStatus('Running', [TimeSpan]::FromSeconds(30))

$health = $null
for ($attempt = 0; $attempt -lt 20 -and -not $health; $attempt++) {
    try { $health = Invoke-RestMethod -Uri "${url}api/health" -TimeoutSec 2 } catch { Start-Sleep -Milliseconds 500 }
}

Write-Host ''
Write-Host "Installed. Service: $((Get-Service -Name $serviceName).Status)" -ForegroundColor Green
if ($health) {
    Write-Host "Dashboard $($health.version) at $url - database: $($health.storage)"
    if ($health.storage -ne 'Ready') { Write-Warning "The dashboard cannot read the database yet ($($health.storage)). Is the collector installed and running?" }
}
else {
    Write-Warning "The dashboard did not answer on $url. See the Application event log (source $serviceName)."
}
