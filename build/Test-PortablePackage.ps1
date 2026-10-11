[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $ZipPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$zip = [System.IO.Path]::GetFullPath($ZipPath)
if (-not (Test-Path -LiteralPath $zip -PathType Leaf)) {
    throw "Portable ZIP does not exist: $zip"
}

$temp = Join-Path ([System.IO.Path]::GetTempPath()) (
    "ОЦИФРОВКА portable QA " + [guid]::NewGuid().ToString('N')
)

try {
    Expand-Archive -LiteralPath $zip -DestinationPath $temp -Force

    $exe = Join-Path $temp 'ОЦИФРОВКА.exe'
    $ocr = Join-Path $temp 'Engine/OCR/tesseract.exe'
    $models = Join-Path $temp 'Engine/OCR/tessdata'

    foreach ($required in @(
        $exe,
        $ocr,
        (Join-Path $temp 'coreclr.dll'),
        (Join-Path $temp 'hostfxr.dll'),
        (Join-Path $temp 'hostpolicy.dll')
    )) {
        if (-not (Test-Path -LiteralPath $required -PathType Leaf)) {
            throw "Portable self-contained package is missing: $required"
        }
    }

    foreach ($language in @('ukr', 'eng', 'rus', 'pol', 'deu', 'osd')) {
        $model = Join-Path $models ($language + '.traineddata')
        if (-not (Test-Path -LiteralPath $model -PathType Leaf)) {
            throw "Missing bundled OCR language model: $language"
        }
    }

    foreach ($privatePath in @(
        (Join-Path $temp 'Data/digitizer.db'),
        (Join-Path $temp 'Workspace/Input'),
        (Join-Path $temp 'Workspace/Temp'),
        (Join-Path $temp 'Logs')
    )) {
        if (Test-Path -LiteralPath $privatePath) {
            throw "Runtime/private data was accidentally bundled: $privatePath"
        }
    }

    # A real OCR invocation after ZIP extraction proves that native DLLs
    # and tessdata still work without an installer, even when the install
    # path contains Unicode characters and spaces.
    & (Join-Path $PSScriptRoot 'Smoke-Test-Local-Ocr.ps1') -ApplicationDirectory $temp

    if ($LASTEXITCODE -ne 0) {
        throw "Extracted portable OCR acceptance exited with $LASTEXITCODE"
    }

    Write-Host 'PASS: extracted portable ZIP is self-contained and works from a Unicode path.'
}
finally {
    if (Test-Path -LiteralPath $temp -PathType Container) {
        Remove-Item -LiteralPath $temp -Recurse -Force
    }
}
