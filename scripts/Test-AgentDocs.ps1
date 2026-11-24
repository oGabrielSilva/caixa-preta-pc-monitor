<#
.SYNOPSIS
    Checks that agent instructions follow the repository convention.

.DESCRIPTION
    - Every folder directly under src/ and tests/ has an AGENTS.md.
    - Every AGENTS.md has a sibling CLAUDE.md whose content is exactly "@AGENTS.md".
    Exits with code 1 and lists every violation, or 0 when the convention holds.
#>
[CmdletBinding()]
param(
    [string] $Root = (Split-Path -Parent $PSScriptRoot)
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ignoredSegments = @('node_modules', 'bin', 'obj', '.nuxt', '.output', '.git', '.superpowers', 'artifacts')
$violations = [System.Collections.Generic.List[string]]::new()

function Test-IsIgnored([string] $path) {
    $segments = $path.Substring($Root.Length).Split([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    return [bool]($segments | Where-Object { $ignoredSegments -contains $_ })
}

function Get-RelativePath([string] $path) {
    return [IO.Path]::GetRelativePath($Root, $path)
}

foreach ($area in 'src', 'tests') {
    $areaPath = Join-Path $Root $area
    if (-not (Test-Path $areaPath)) { continue }

    foreach ($project in Get-ChildItem -Path $areaPath -Directory) {
        if (-not (Test-Path (Join-Path $project.FullName 'AGENTS.md'))) {
            $violations.Add("Missing AGENTS.md: $(Get-RelativePath $project.FullName)")
        }
    }
}

$agentFiles = Get-ChildItem -Path $Root -Filter 'AGENTS.md' -Recurse -File |
    Where-Object { -not (Test-IsIgnored $_.FullName) }

foreach ($agents in $agentFiles) {
    $claude = Join-Path $agents.DirectoryName 'CLAUDE.md'
    if (-not (Test-Path $claude)) {
        $violations.Add("Missing CLAUDE.md next to: $(Get-RelativePath $agents.FullName)")
    }
    elseif ((Get-Content -Raw $claude).Trim() -ne '@AGENTS.md') {
        $violations.Add("CLAUDE.md must contain exactly '@AGENTS.md': $(Get-RelativePath $claude)")
    }
}

if ($violations.Count -gt 0) {
    $violations | ForEach-Object { Write-Host "  - $_" }
    Write-Host "Agent docs check failed: $($violations.Count) violation(s)."
    exit 1
}

Write-Host "Agent docs check passed: $($agentFiles.Count) AGENTS.md file(s)."
exit 0
