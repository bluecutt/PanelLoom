param([string]$Version='0.2.0-preview.4')
$ErrorActionPreference='Stop'
if($Version -notmatch '^\d+\.\d+\.\d+(-[a-z0-9.-]+)?$'){throw 'Invalid version'}
$nativeRoot=Split-Path -Parent $PSScriptRoot
$target=Join-Path $nativeRoot ('dist/ComicEditor-source-'+$Version)
if(Test-Path -LiteralPath $target){throw 'Source candidate exists; use a new version'}
New-Item -ItemType Directory -Path $target | Out-Null
$codeExtensions=@('.cs','.xaml','.csproj','.props','.sln','.md','.html','.json','.ps1','.csv','.yaml','.config')
$neutralImages=@('examples\minimal-page\art.png','examples\minimal-page\balloon.png','skills\comic-page-workflow\assets\example-page\art.png','skills\comic-page-workflow\assets\example-page\balloon.png','docs\images\neutral\assembled-example.png')
$discussion='docs\2026-10-01-'+[string]::Concat([char]0x4EA4,[char]0x4E92,[char]0x95EE,[char]0x9898,[char]0x6838,[char]0x9A8C,[char]0x4E0E,[char]0x4F18,[char]0x5316,[char]0x8BA8,[char]0x8BBA)+'.md'
$excludedDocs=@('docs\2026-10-01-usability-upgrade-progress.md',$discussion,'docs\2026-10-01-usability-upgrade-progress.html',($discussion -replace '\.md$','.html'))
foreach($directory in @('src','tests','scripts','docs','schemas','skills','examples')) {
 $source=Join-Path $nativeRoot $directory
 foreach($file in Get-ChildItem -LiteralPath $source -Recurse -File) {
  $relative=$file.FullName.Substring($nativeRoot.Length+1)
  if($relative -in $excludedDocs){continue}
  if($relative -match '(^|\\)(bin|obj)(\\|$)' -or ($file.Attributes -band [IO.FileAttributes]::ReparsePoint)){continue}
  if($file.Extension -notin $codeExtensions -and $relative -notin $neutralImages){continue}
  $destination=Join-Path $target $relative;[void][IO.Directory]::CreateDirectory((Split-Path -Parent $destination));Copy-Item -LiteralPath $file.FullName -Destination $destination
 }
}
& (Join-Path $PSScriptRoot 'Copy-PublicRootDocs.ps1') -SourceDirectory $nativeRoot -DestinationDirectory $target
foreach($file in @('ComicEditor.sln','Directory.Build.props','global.json','NuGet.Config','.gitignore')){Copy-Item -LiteralPath (Join-Path $nativeRoot $file) -Destination $target}
& (Join-Path $PSScriptRoot 'Scan-Public-Package.ps1') -Directory $target
Compress-Archive -LiteralPath $target -DestinationPath ($target+'.zip')
Write-Output $target
