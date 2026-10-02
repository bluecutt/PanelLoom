param([string]$OutputDirectory='artifacts/dev-pair')
$ErrorActionPreference='Stop'
$nativeRoot=Split-Path -Parent $PSScriptRoot
$dotnet=& (Join-Path $PSScriptRoot 'Resolve-Dotnet.ps1')
Push-Location $nativeRoot
try {
 foreach($project in @('src/ComicEditor.Desktop','src/ComicEditor.Cli')) {
  & $dotnet restore $project --configfile NuGet.Config --verbosity quiet
  if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
  & $dotnet build $project --no-restore --verbosity quiet
  if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
 }
 $target=[IO.Path]::GetFullPath((Join-Path $nativeRoot $OutputDirectory))
 if(-not $target.StartsWith((Join-Path $nativeRoot 'artifacts\'),[StringComparison]::OrdinalIgnoreCase)){throw 'Dev pair must stay in artifacts'}
 if(Test-Path -LiteralPath $target){throw 'Use a new dev-pair directory'}
 New-Item -ItemType Directory -Path $target | Out-Null
 foreach($project in @('ComicEditor.Desktop','ComicEditor.Cli')) {
  $source=Join-Path $nativeRoot ('src/'+$project+'/bin/Debug/net10.0-windows')
  foreach($file in Get-ChildItem -LiteralPath $source -File) {
   $destination=Join-Path $target $file.Name
   if(Test-Path -LiteralPath $destination){if((Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath $destination).Hash){throw ('Shared file mismatch: '+$file.Name)}}
   else {Copy-Item -LiteralPath $file.FullName -Destination $destination}
  }
 }
 Write-Output $target
}finally{Pop-Location}
