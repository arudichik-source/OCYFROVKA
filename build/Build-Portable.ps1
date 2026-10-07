$ErrorActionPreference = 'Stop'

function Assert-LastExitCode {
    param([string]$Step)

    if ($LASTEXITCODE -ne 0) {
        throw "$Step failed with exit code $LASTEXITCODE"
    }
}

$root = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $root 'publish/OCYFROVKA'
$zipPath = Join-Path $root 'OCYFROVKA_Portable_x64.zip'

if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

dotnet restore (Join-Path $root 'src/Ocyfrovka.App/Ocyfrovka.App.csproj')
Assert-LastExitCode 'App restore'

dotnet restore (Join-Path $root 'tests/Ocyfrovka.Core.Tests/Ocyfrovka.Core.Tests.csproj')
Assert-LastExitCode 'Core tests restore'

dotnet restore (Join-Path $root 'tests/Ocyfrovka.App.Tests/Ocyfrovka.App.Tests.csproj')
Assert-LastExitCode 'Windows integration tests restore'

dotnet test (Join-Path $root 'tests/Ocyfrovka.Core.Tests/Ocyfrovka.Core.Tests.csproj') -c Release --no-restore
Assert-LastExitCode 'Core unit tests'

dotnet test (Join-Path $root 'tests/Ocyfrovka.App.Tests/Ocyfrovka.App.Tests.csproj') -c Release --no-restore
Assert-LastExitCode 'Windows integration tests'

dotnet publish (Join-Path $root 'src/Ocyfrovka.App/Ocyfrovka.App.csproj') `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=false `
  -p:PublishTrimmed=false `
  -o $publishDir
Assert-LastExitCode 'Publish'

& (Join-Path $PSScriptRoot 'Prepare-OcrRuntime.ps1') -Destination (Join-Path $publishDir 'Engine/OCR')
Assert-LastExitCode 'OCR runtime preparation'

@'
ОЦИФРОВКА Portable x64

Розпакуйте архів у папку, доступну для запису, та запустіть ОЦИФРОВКА.exe.
Встановлення .NET або права адміністратора не потрібні.
'@ | Set-Content -Path (Join-Path $publishDir 'README_PORTABLE.txt') -Encoding UTF8

Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $zipPath -CompressionLevel Optimal
Write-Host "Created: $zipPath"
