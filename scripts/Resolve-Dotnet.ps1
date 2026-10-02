$ErrorActionPreference = 'Stop'
$nativeRoot = Split-Path -Parent $PSScriptRoot
$env:DOTNET_CLI_HOME = Join-Path $nativeRoot '.tools/cli-home'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = 'false'
$privateSdk = Join-Path $nativeRoot '.tools/dotnet/dotnet.exe'
if($env:COMIC_EDITOR_DOTNET_PATH){$privateSdk=[IO.Path]::GetFullPath($env:COMIC_EDITOR_DOTNET_PATH)}
if (-not (Test-Path -LiteralPath $privateSdk)) { throw 'Private .NET 10 SDK missing. Run Prepare-Sdk.ps1.' }
$version = & $privateSdk --version
if ($LASTEXITCODE -ne 0 -or $version -notmatch '^10\.') { throw "Expected stable .NET 10 SDK; got $version" }
$privateSdk
