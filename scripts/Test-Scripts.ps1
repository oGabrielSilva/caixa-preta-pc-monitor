<#
.SYNOPSIS
    Checks that every install script runs on Windows PowerShell 5.1, the shell the installer uses.

.DESCRIPTION
    Run it with powershell.exe (5.1) so its parser is the one that matters. For each .ps1/.psm1 under scripts/:
    - no parse errors (PowerShell 7 syntax such as ?? or ternaries fails here);
    - no '#Requires -Version' above 5.1;
    - ASCII only: 5.1 reads a file without BOM as ANSI, so any other character turns into garbage;
    - no $PSScriptRoot in a parameter default: 5.1 leaves it empty there in a [CmdletBinding()] script.
    Exits 1 on the first problem list.

.EXAMPLE
    powershell.exe -NoProfile -File scripts/Test-Scripts.ps1
#>
[CmdletBinding()]
param(
    [string] $Path
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# $PSScriptRoot is empty in parameter defaults on Windows PowerShell 5.1, so defaults that need it are set here.
if (-not $Path) { $Path = $PSScriptRoot }

$problems = New-Object System.Collections.Generic.List[string]
$files = Get-ChildItem -Path $Path -Recurse -Include '*.ps1', '*.psm1' -File

foreach ($file in $files) {
    $tokens = $null
    $errors = $null
    $ast = [System.Management.Automation.Language.Parser]::ParseFile($file.FullName, [ref] $tokens, [ref] $errors)
    foreach ($parseError in $errors) {
        $problems.Add("$($file.Name):$($parseError.Extent.StartLineNumber): $($parseError.Message)")
    }

    $required = $ast.ScriptRequirements
    if ($required -and $required.RequiredPSVersion -and $required.RequiredPSVersion -gt [version] '5.1') {
        $problems.Add("$($file.Name): requires PowerShell $($required.RequiredPSVersion); the installer runs Windows PowerShell 5.1.")
    }

    if ($ast.ParamBlock) {
        $defaults = $ast.ParamBlock.Parameters | Where-Object { $_.DefaultValue } | ForEach-Object { $_.DefaultValue }
        foreach ($default in $defaults) {
            $usesRoot = $default.FindAll({ param($node) $node -is [System.Management.Automation.Language.VariableExpressionAst] -and $node.VariablePath.UserPath -eq 'PSScriptRoot' }, $true)
            if (@($usesRoot).Count -gt 0) {
                $problems.Add("$($file.Name):$($default.Extent.StartLineNumber): `$PSScriptRoot in a parameter default is empty on Windows PowerShell 5.1; resolve it in the script body.")
            }
        }
    }

    $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
    $line = 1
    foreach ($byte in $bytes) {
        if ($byte -eq 10) { $line++ }
        if ($byte -gt 127) {
            $problems.Add("$($file.Name):${line}: non-ASCII character; Windows PowerShell 5.1 would misread it.")
            break
        }
    }
}

if ($problems.Count -gt 0) {
    $problems | ForEach-Object { Write-Host $_ -ForegroundColor Red }
    exit 1
}

Write-Host "$($files.Count) scripts are compatible with Windows PowerShell $($PSVersionTable.PSVersion)." -ForegroundColor Green
