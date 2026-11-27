<#
.SYNOPSIS
    Renders the LinkedIn carousel (slides.html) into carrossel-caixa-preta.pdf and one PNG per slide (1080 x 1350).

.DESCRIPTION
    Uses Microsoft Edge in headless mode. Needs network access for the Google Sans Flex font (Segoe UI otherwise).

.EXAMPLE
    pwsh docs/divulgacao/carrossel/build-carousel.ps1
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$edge = "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe"
$source = Join-Path $PSScriptRoot 'slides.html'
$url = ([Uri] $source).AbsoluteUri
$out = Join-Path $PSScriptRoot 'png'
New-Item -ItemType Directory -Force $out | Out-Null
$profileDir = Join-Path ([IO.Path]::GetTempPath()) 'caixa-preta-carousel-edge'
$common = @('--headless=new', '--disable-gpu', '--hide-scrollbars', '--allow-file-access-from-files', "--user-data-dir=$profileDir", '--virtual-time-budget=8000')

$pdf = Join-Path $PSScriptRoot 'carrossel-caixa-preta.pdf'
& $edge @common '--no-pdf-header-footer' "--print-to-pdf=$pdf" $url 2>$null | Out-Null
Write-Host $pdf

$slides = ([regex]::Matches((Get-Content $source -Raw), 'class="slide" id="s(\d+)"')).Count
foreach ($n in 1..$slides) {
    $png = Join-Path $out ("slide-{0}.png" -f $n)
    & $edge @common '--window-size=1080,1350' "--screenshot=$png" "${url}?s=$n" 2>$null | Out-Null
    Write-Host $png
}
