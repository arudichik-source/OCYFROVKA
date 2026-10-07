param(
    [Parameter(Mandatory = $true)]
    [string]$Destination,
    [string]$LockFile = (Join-Path $PSScriptRoot 'ocr-runtime.lock.json')
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

function Assert-Sha256 {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Expected,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $actual = (Get-FileHash -Path $Path -Algorithm SHA256).Hash.ToLowerInvariant()
    $expectedNormalized = $Expected.Trim().ToLowerInvariant()

    if ($expectedNormalized -and $actual -ne $expectedNormalized) {
        throw "$Label SHA256 mismatch. Expected $expectedNormalized, got $actual."
    }

    return $actual
}

$lock = Get-Content -LiteralPath $LockFile -Raw | ConvertFrom-Json
$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ("ocyfrovka-ocr-" + [Guid]::NewGuid().ToString('N'))
$installer = Join-Path $tempRoot 'tesseract-setup.exe'
$installRoot = Join-Path $tempRoot 'install'

try {
    New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null
    New-Item -ItemType Directory -Path $installRoot -Force | Out-Null

    Write-Host "Downloading Tesseract $($lock.runtime.version)..."
    Invoke-WebRequest -Uri $lock.runtime.installerUrl -OutFile $installer
    $runtimeHash = Assert-Sha256 -Path $installer -Expected $lock.runtime.installerSha256 -Label 'Tesseract installer'
    Write-Host "TESSERACT_INSTALLER_SHA256 $runtimeHash"

    $arguments = @('/S', "/D=$installRoot")
    $process = Start-Process -FilePath $installer -ArgumentList $arguments -Wait -PassThru

    if ($process.ExitCode -ne 0) {
        throw "Tesseract installer failed with exit code $($process.ExitCode)."
    }

    $installedExe = Join-Path $installRoot 'tesseract.exe'
    if (-not (Test-Path -LiteralPath $installedExe)) {
        throw "Tesseract silent install completed but tesseract.exe was not found at $installedExe"
    }

    if (Test-Path -LiteralPath $Destination) {
        Remove-Item -LiteralPath $Destination -Recurse -Force
    }

    New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    Copy-Item -Path (Join-Path $installRoot '*') -Destination $Destination -Recurse -Force

    Get-ChildItem -LiteralPath $Destination -Filter 'unins*.exe' -File -ErrorAction SilentlyContinue | Remove-Item -Force
    Get-ChildItem -LiteralPath $Destination -Filter 'unins*.dat' -File -ErrorAction SilentlyContinue | Remove-Item -Force

    $tessdata = Join-Path $Destination 'tessdata'
    New-Item -ItemType Directory -Path $tessdata -Force | Out-Null
    Get-ChildItem -LiteralPath $tessdata -Filter '*.traineddata' -File -ErrorAction SilentlyContinue | Remove-Item -Force

    $resolvedModels = @()

    foreach ($model in $lock.models) {
        $target = Join-Path $tessdata ($model.code + '.traineddata')
        Write-Host "Downloading OCR model $($model.code)..."
        Invoke-WebRequest -Uri $model.url -OutFile $target

        $hash = Assert-Sha256 -Path $target -Expected ([string]$model.sha256) -Label ("OCR model " + $model.code)
        $size = (Get-Item -LiteralPath $target).Length
        Write-Host "MODEL_SHA256 $($model.code) $hash $size"

        $resolvedModels += [ordered]@{
            code = $model.code
            sha256 = $hash
            size = $size
            source = $model.url
        }
    }

    $licenses = Join-Path $Destination 'LICENSES'
    New-Item -ItemType Directory -Path $licenses -Force | Out-Null
    Invoke-WebRequest -Uri 'https://raw.githubusercontent.com/tesseract-ocr/tesseract/5.5.3/LICENSE' -OutFile (Join-Path $licenses 'Tesseract-LICENSE.txt')
    Invoke-WebRequest -Uri ('https://raw.githubusercontent.com/tesseract-ocr/tessdata_best/' + $lock.modelSources.tessdataBestCommit + '/LICENSE') -OutFile (Join-Path $licenses 'tessdata_best-LICENSE.txt')
    Invoke-WebRequest -Uri ('https://raw.githubusercontent.com/tesseract-ocr/tessdata/' + $lock.modelSources.tessdataCommit + '/LICENSE') -OutFile (Join-Path $licenses 'tessdata-LICENSE.txt')

    $manifest = [ordered]@{
        schemaVersion = 1
        runtimeVersion = $lock.runtime.version
        runtimeInstallerSha256 = $runtimeHash
        generatedUtc = [DateTime]::UtcNow.ToString('O')
        tessdataBestCommit = $lock.modelSources.tessdataBestCommit
        tessdataCommit = $lock.modelSources.tessdataCommit
        models = $resolvedModels
    }

    $manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $Destination 'runtime-manifest.json') -Encoding UTF8

    $runtimeExe = Join-Path $Destination 'tesseract.exe'
    & $runtimeExe --version
    if ($LASTEXITCODE -ne 0) {
        throw "Packaged tesseract.exe --version failed with exit code $LASTEXITCODE."
    }

    & $runtimeExe --tessdata-dir $tessdata --list-langs
    if ($LASTEXITCODE -ne 0) {
        throw "Packaged Tesseract language listing failed with exit code $LASTEXITCODE."
    }

    Write-Host "OCR_RUNTIME_READY $Destination"
}
finally {
    if (Test-Path -LiteralPath $tempRoot) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
