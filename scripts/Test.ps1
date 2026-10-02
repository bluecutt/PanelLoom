param([string]$Suite = 'All')
$ErrorActionPreference = 'Stop'
$nativeRoot = Split-Path -Parent $PSScriptRoot
$dotnet = & (Join-Path $PSScriptRoot 'Resolve-Dotnet.ps1')
Push-Location $nativeRoot
try {
    if ($Suite.StartsWith('Ui')) {
        & $dotnet restore 'tests/ComicEditor.UiTests' --configfile 'NuGet.Config' --verbosity quiet
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        & $dotnet run --no-restore --project 'tests/ComicEditor.UiTests' -- --suite $Suite
        exit $LASTEXITCODE
    }
    & $dotnet restore 'tests/ComicEditor.Tests' --configfile 'NuGet.Config' --verbosity quiet
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    & $dotnet run --no-restore --project 'tests/ComicEditor.Tests' -- --suite $Suite
    exit $LASTEXITCODE
} finally { Pop-Location }
