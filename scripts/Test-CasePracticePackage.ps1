param([Parameter(Mandatory=$true)][string]$Directory,[Parameter(Mandatory=$true)][string]$CliPath)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath($Directory).TrimEnd('\')+'\'
function Inside($relative){if([IO.Path]::IsPathRooted($relative) -or $relative -match ':|^[/\\]'){throw 'Absolute case reference rejected'};$p=[IO.Path]::GetFullPath((Join-Path $root $relative));if(-not $p.StartsWith($root,[StringComparison]::OrdinalIgnoreCase)){throw 'Case reference escapes root'};return $p}
function ReadJson($p){return [IO.File]::ReadAllText($p)|ConvertFrom-Json}
function Fingerprint($p){return (Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash}
foreach($name in @('asset-manifest.json','README.md','ASSET-TERMS.md')){if(-not(Test-Path -LiteralPath (Inside $name) -PathType Leaf)){throw ('Required case file missing: '+$name)}}
$m=ReadJson (Inside 'asset-manifest.json')
if($m.schemaVersion -ne 1 -or $m.usageTerms -ne 'local-layout-only' -or -not @($m.cases).Count){throw 'Invalid case manifest'}
Add-Type -AssemblyName System.Drawing
$ids=@{};$listed=@{}
foreach($case in $m.cases){
 if($case.caseId -notmatch '^[A-Z0-9_-]+$' -or $ids.ContainsKey($case.caseId) -or $case.textStatus -notin @('lettered','blank')){throw 'Invalid case identity'};$ids[$case.caseId]=$true
 foreach($a in @($case.assets)+@($case.references)){
  if($a.usageTerms -ne 'local-layout-only' -or -not $a.role -or $a.sha256 -notmatch '^[A-Fa-f0-9]{64}$'){throw 'Asset terms/provenance missing'}
  $p=Inside $a.path;if(-not(Test-Path -LiteralPath $p -PathType Leaf) -or (Fingerprint $p) -ne $a.sha256){throw 'Missing or changed case asset'}
  $bitmap=[Drawing.Bitmap]::FromFile($p);try{if($bitmap.Width -ne $a.width -or $bitmap.Height -ne $a.height){throw 'Asset dimensions mismatch'}}finally{$bitmap.Dispose()}
  $listed[$p]=$a
 }
 if(@($case.projects).Count -ne 2){throw 'Start/reference projects required'}
 foreach($item in $case.projects){
  $p=Inside $item.path;$doc=ReadJson $p
  if(@($doc.panels).Count -ne $case.panelCount -or @($doc.balloons).Count -ne $case.balloonCount -or $item.panelCount -ne $case.panelCount -or $item.balloonCount -ne $case.balloonCount){throw 'Project object count mismatch'}
  if($doc.format -ne 'ComicPanelEditorProject' -or $doc.version -ne 2){throw 'Wrong editor format'}
  foreach($relative in @($doc.assetBase,$doc.outputDirectory)){if($relative -and ([IO.Path]::IsPathRooted($relative) -or $relative -match ':|^[/\\]')){throw 'Absolute project base rejected'}}
  $base=[IO.Path]::GetFullPath((Join-Path (Split-Path -Parent $p) $doc.assetBase));if(-not $base.StartsWith($root,[StringComparison]::OrdinalIgnoreCase)){throw 'Asset base escapes case root'}
  foreach($kind in @('panel','balloon')){$objects=if($kind -eq 'panel'){$doc.panels}else{$doc.balloons};foreach($o in $objects){
   if([IO.Path]::IsPathRooted($o.sourceImage) -or $o.sourceImage -match ':|^[/\\]'){throw 'Absolute image rejected'}
   $source=[IO.Path]::GetFullPath((Join-Path $base $o.sourceImage));if(-not $source.StartsWith($root,[StringComparison]::OrdinalIgnoreCase) -or -not $listed.ContainsKey($source)){throw 'Image not in case manifest'}
   $a=$listed[$source];if($a.kind -ne $kind -or $a.id -ne $o.id){throw 'Asset/object mapping mismatch'}
  }}
  if((Fingerprint $p) -ne $item.sha256){throw 'Project hash mismatch'}
  $raw=& $CliPath validate --project $p;$exit=$LASTEXITCODE;$r=($raw -join "`n")|ConvertFrom-Json;if($exit -ne 0 -or -not $r.success){throw 'CLI validation failed'}
 }
}
foreach($file in Get-ChildItem -LiteralPath $root -Recurse -File){
 if($file.Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Redirected case file'}
 if($file.Extension -in @('.png','.jpg','.jpeg','.webp','.gif') -and -not $listed.ContainsKey($file.FullName)){throw 'Unlisted case image'}
 if($file.Extension -in @('.json','.md','.txt')){$content=[IO.File]::ReadAllText($file.FullName);if($content -match '(?i)[A-Z]:[\\/]+Users[\\/]|[A-Z]:[\\/]+comic_create|data:image/[^;]+;base64,|sk-[A-Za-z0-9_-]{20,}|ghp_[A-Za-z0-9]{20,}'){throw 'Private text in case package'}}
}
Write-Output ('CASE_PACKAGE_PASS '+$m.cases.Count+' cases; '+$listed.Count+' images')
