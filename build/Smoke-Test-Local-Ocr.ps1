[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $ApplicationDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = [System.IO.Path]::GetFullPath($ApplicationDirectory)
$engine = Join-Path $root 'Engine/OCR/tesseract.exe'
$tessdata = Join-Path $root 'Engine/OCR/tessdata'

if (-not (Test-Path -LiteralPath $engine -PathType Leaf)) {
    throw "Portable Tesseract executable is missing: $engine"
}
if (-not (Test-Path -LiteralPath $tessdata -PathType Container)) {
    throw "Portable tessdata directory is missing: $tessdata"
}

Add-Type -AssemblyName System.Drawing
$temp = Join-Path ([System.IO.Path]::GetTempPath()) ("ocyfrovka-ci-ocr-" + [guid]::NewGuid().ToString('N'))
[System.IO.Directory]::CreateDirectory($temp) | Out-Null

function New-SyntheticTestImage {
    param(
        [Parameter(Mandatory = $true)][string] $Text,
        [Parameter(Mandatory = $true)][string] $FilePath
    )

    $bitmap = [System.Drawing.Bitmap]::new(2000, 360)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $font = [System.Drawing.Font]::new(
        'Arial',
        100,
        [System.Drawing.FontStyle]::Bold,
        [System.Drawing.GraphicsUnit]::Pixel
    )
    try {
        $bitmap.SetResolution(300, 300)
        $graphics.PageUnit = [System.Drawing.GraphicsUnit]::Pixel
        $graphics.Clear([System.Drawing.Color]::White)
        $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
        $graphics.DrawString(
            $Text,
            $font,
            [System.Drawing.Brushes]::Black,
            [single]60,
            [single]90
        )
        $bitmap.Save($FilePath, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $font.Dispose()
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

function Invoke-SyntheticRecognition {
    param(
        [Parameter(Mandatory = $true)][string] $FilePath,
        [Parameter(Mandatory = $true)][string] $Language,
        [Parameter(Mandatory = $true)][string] $ExpectedDigits
    )

    $output = & $engine $FilePath stdout --tessdata-dir $tessdata -l $Language --psm 6 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Tesseract synthetic test failed for $Language (exit $LASTEXITCODE): $output"
    }

    $recognized = (($output | Out-String) -replace '\s+', ' ').Trim()
    Write-Host "Synthetic OCR ($Language): $recognized"

    if (($recognized -replace '\s+', '') -notmatch [regex]::Escape($ExpectedDigits)) {
        throw "Expected test digits '$ExpectedDigits' missing from OCR output ($Language)."
    }
}

try {
    $english = Join-Path $temp 'english.png'
    $ukrainian = Join-Path $temp 'ukrainian.png'

    New-SyntheticTestImage -Text 'INVOICE 12345' -FilePath $english
    New-SyntheticTestImage -Text 'ТЕСТ 67890' -FilePath $ukrainian

    Invoke-SyntheticRecognition -FilePath $english -Language 'eng' -ExpectedDigits '12345'
    Invoke-SyntheticRecognition -FilePath $ukrainian -Language 'ukr' -ExpectedDigits '67890'

    Write-Host 'PASS: Portable OCR recognized synthetic English and Ukrainian samples offline.'
}
finally {
    if (Test-Path -LiteralPath $temp -PathType Container) {
        Remove-Item -LiteralPath $temp -Recurse -Force
    }
}
