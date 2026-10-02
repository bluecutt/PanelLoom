param([switch]$Download)
$ErrorActionPreference = 'Stop'
$nativeRoot = Split-Path -Parent $PSScriptRoot
$sdkVersion = '10.0.401'
$expected = '24b670ad3d923bfcf47df6c3b034152398b42f6dbc388e10d783aee1cfb5e5817d399fc0ae2a12cfa822a55e61d34830ccb15c50ef6efee437ab874bb7c79430'
$archive = Join-Path $nativeRoot ".tools/downloads/dotnet-sdk-$sdkVersion-win-x64.zip"
$destination = Join-Path $nativeRoot '.tools/dotnet'
if (Test-Path (Join-Path $destination 'dotnet.exe')) { & (Join-Path $PSScriptRoot 'Resolve-Dotnet.ps1'); exit 0 }
if (-not (Test-Path $archive)) {
    if (-not $Download) { throw 'Archive not present. Use -Download for explicit network preparation.' }
    [void][IO.Directory]::CreateDirectory((Split-Path -Parent $archive))
    Invoke-WebRequest -UseBasicParsing "https://builds.dotnet.microsoft.com/dotnet/Sdk/$sdkVersion/dotnet-sdk-$sdkVersion-win-x64.zip" -OutFile $archive
}
if ((Get-FileHash -LiteralPath $archive -Algorithm SHA512).Hash -ne $expected) { throw 'SDK archive SHA512 mismatch; no extraction.' }
[void][IO.Directory]::CreateDirectory($destination)
Expand-Archive -LiteralPath $archive -DestinationPath $destination
& (Join-Path $PSScriptRoot 'Resolve-Dotnet.ps1')
