param([string]$Version='0.2.0-preview.4',[string]$OutputDirectory,[switch]$Offline)
$ErrorActionPreference='Stop'
if($Version -notmatch '^\d+\.\d+\.\d+(-[a-z0-9.-]+)?$'){throw 'Invalid version'}
$nativeRoot=Split-Path -Parent $PSScriptRoot
$discussion='docs\2026-10-01-'+[string]::Concat([char]0x4EA4,[char]0x4E92,[char]0x95EE,[char]0x9898,[char]0x6838,[char]0x9A8C,[char]0x4E0E,[char]0x4F18,[char]0x5316,[char]0x8BA8,[char]0x8BBA)+'.md'
$excludedDocs=@('docs\2026-10-01-usability-upgrade-progress.md',$discussion,'docs\2026-10-01-usability-upgrade-progress.html',($discussion -replace '\.md$','.html'))
$dotnet=& (Join-Path $PSScriptRoot 'Resolve-Dotnet.ps1')
$env:NUGET_PACKAGES=Join-Path $nativeRoot '.tools/nuget-packages'
$target=if($OutputDirectory){[IO.Path]::GetFullPath($OutputDirectory)}else{Join-Path $nativeRoot ('dist/ComicEditor-'+$Version+'-win-x64')}
if(-not $target.StartsWith((Join-Path $nativeRoot 'dist\'),[StringComparison]::OrdinalIgnoreCase)){throw 'Release destination must stay inside dist'}
if(Test-Path -LiteralPath $target){throw 'Release exists; use a new version/destination'}
$stage=Join-Path $nativeRoot ('artifacts/release-staging/'+[Guid]::NewGuid().ToString())
[void][IO.Directory]::CreateDirectory($stage)
Push-Location $nativeRoot
try {
 foreach($name in @('Desktop','Cli')) {
  $project='src/ComicEditor.'+$name
  if($Offline){& $dotnet restore $project -r win-x64 -p:SelfContained=true --configfile NuGet.Config --verbosity quiet}
  else{& $dotnet restore $project -r win-x64 -p:SelfContained=true --source https://api.nuget.org/v3/index.json --verbosity quiet}
  if($LASTEXITCODE -ne 0){throw 'Runtime restore failed'}
  & $dotnet publish $project --no-restore -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false -p:Version=$Version -p:DebugType=None -o (Join-Path $stage $name) --verbosity quiet
  if($LASTEXITCODE -ne 0){throw 'Self-contained publish failed'}
 }
 $merged=Join-Path $stage 'merged'
 & (Join-Path $PSScriptRoot 'Merge-Publish.ps1') -First (Join-Path $stage 'Desktop') -Second (Join-Path $stage 'Cli') -Destination $merged
 foreach($directory in @('docs','schemas','examples','skills')){foreach($file in Get-ChildItem -LiteralPath (Join-Path $nativeRoot $directory) -Recurse -File){$relative=$file.FullName.Substring($nativeRoot.Length+1);if($relative -in $excludedDocs){continue};$destination=Join-Path $merged $relative;[void][IO.Directory]::CreateDirectory((Split-Path -Parent $destination));Copy-Item -LiteralPath $file.FullName -Destination $destination}}
 & (Join-Path $PSScriptRoot 'Copy-PublicRootDocs.ps1') -SourceDirectory $nativeRoot -DestinationDirectory $merged
 & (Join-Path $PSScriptRoot 'Copy-RuntimeNotices.ps1') -PublishDirectory $merged -CacheDirectory $env:NUGET_PACKAGES -Destination $merged -SupplementalNotices (Join-Path (Split-Path -Parent $dotnet) 'ThirdPartyNotices.txt')
 & (Join-Path $PSScriptRoot 'Scan-Public-Package.ps1') -Directory $merged
 $entries=@(Get-ChildItem -LiteralPath $merged -Recurse -File | Sort-Object FullName | ForEach-Object{[ordered]@{path=$_.FullName.Substring($merged.Length+1).Replace('\','/');sha256=(Get-FileHash -LiteralPath $_.FullName).Hash;bytes=$_.Length}})
 # Generated manifest is a build artifact, not source configuration or user data.
 [IO.File]::WriteAllText((Join-Path $merged 'release-manifest.json'),([ordered]@{version=$Version;rid='win-x64';selfContained=$true;files=$entries}|ConvertTo-Json -Depth 8),(New-Object Text.UTF8Encoding $false))
 [void][IO.Directory]::CreateDirectory((Split-Path -Parent $target))
 Move-Item -LiteralPath $merged -Destination $target
 $zip=$target+'.zip';if(Test-Path -LiteralPath $zip){throw 'ZIP already exists; package directory is ready but not overwritten'}
 Compress-Archive -LiteralPath $target -DestinationPath $zip
 Write-Output $target
}finally{Pop-Location}
